using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tracker.App.Common.Auth;
using Tracker.App.Common.Constants;
using Tracker.App.Common.Models;
using Tracker.App.Common.Routing;
using Tracker.App.Features.Auth.DTOs;
using Tracker.App.Features.Auth.Interface;

namespace Tracker.App.Features.Auth.Controller;

/// <summary>
/// Controller handling user authentication, session initiation, and token lifecycle management.
/// 
/// Routing Note:
/// - Inheriting BaseApiController guarantees the route retains the "api" prefix.
/// - [ApiRoute(ApiConstants.AuthRoutePrefix)] produces the route: /api/auth.
/// </summary>
[ApiRoute(ApiConstants.AuthRoutePrefix)]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// Sets an HttpOnly secure cookie for the refresh token and returns the short-lived access token in the JSON body.
    /// Route: POST /api/auth/login
    /// </summary>
    /// <param name="request">Validated login request containing email and password.</param>
    /// <param name="cancellationToken">Cancellation token for aborting the async operation.</param>
    /// <response code="200">Authentication successful. Returns access token, expiry, and user summary wrapped in ApiResponse.</response>
    /// <response code="400">Request validation failed or missing required fields.</response>
    /// <response code="401">Invalid email or password credentials.</response>
    /// <response code="403">User account is inactive or disabled.</response>
    /// <response code="423">User account is temporarily locked out due to excessive failed attempts.</response>
    [HttpPost(ApiConstants.LoginRoute)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        // Extract client network and device context for token auditing & security tracking
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request, clientIp, deviceInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return Failure(result.ErrorMessage!, result.StatusCode);
        }

        // =====================================================================
        // TOKEN MANAGEMENT BEST PRACTICE:
        // Set both Access Token and Refresh Token in HttpOnly secure cookies.
        // This guarantees browser JavaScript (XSS attacks) cannot steal them.
        // =====================================================================
        SetAccessTokenCookie(result.AccessToken!);
        SetRefreshTokenCookie(result.RefreshToken!);

        return Success(result.Data, ApiConstants.LoginSuccessMessage);
    }

    /// <summary>
    /// Rotates the refresh token and mints a new access token without re-entering credentials.
    /// Reads the refresh token securely from the HttpOnly cookie and replaces it with a fresh token.
    /// Route: POST /api/auth/refresh-token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for aborting the async operation.</param>
    /// <response code="200">Token successfully rotated. Returns user profile and session metadata.</response>
    /// <response code="401">Refresh token cookie is missing, invalid, expired, or compromised/reused.</response>
    /// <response code="403">User account is inactive or disabled.</response>
    [HttpPost(ApiConstants.RefreshTokenRoute)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken = default)
    {
        // 1. Extract refresh token from HttpOnly cookie
        if (!Request.Cookies.TryGetValue(ApiConstants.RefreshTokenCookieName, out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            return Failure(ApiConstants.MissingRefreshTokenMessage, StatusCodes.Status401Unauthorized);
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = Request.Headers.UserAgent.ToString();

        // 2. Perform validation, reuse detection, and token rotation
        var result = await _authService.RefreshTokenAsync(refreshToken, clientIp, deviceInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            // Clear invalid/compromised cookies from the client browser
            ClearAuthCookies();
            return Failure(result.ErrorMessage!, result.StatusCode);
        }

        // 3. Issue newly minted access token and rotated refresh token in HttpOnly cookies
        SetAccessTokenCookie(result.AccessToken!);
        SetRefreshTokenCookie(result.RefreshToken!);

        return Success(result.Data, ApiConstants.TokenRefreshedMessage);
    }

    /// <summary>
    /// Logs out the authenticated user, revokes the refresh token session on the server,
    /// and deletes both HttpOnly cookies.
    /// Route: POST /api/auth/logout
    /// </summary>
    /// <param name="allDevices">Optional query parameter. If true, revokes all active refresh tokens for the user across all devices.</param>
    /// <param name="cancellationToken">Cancellation token for aborting the async operation.</param>
    /// <response code="200">Logout successful. Auth cookies removed.</response>
    /// <response code="401">User is unauthenticated or token is invalid.</response>
    [Authorize]
    [HttpPost(ApiConstants.LogoutRoute)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromQuery] bool allDevices = false,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Failure("Unauthorized: Missing or invalid user identity.", StatusCodes.Status401Unauthorized);
        }

        Request.Cookies.TryGetValue(ApiConstants.RefreshTokenCookieName, out var refreshToken);

        // Always delete both auth cookies from the client browser
        ClearAuthCookies();

        await _authService.LogoutAsync(userId, refreshToken, allDevices, cancellationToken);

        _logger.LogInformation("User '{UserId}' logged out successfully (AllDevices: {AllDevices}).", userId, allDevices);

        return SuccessMessage(ApiConstants.LogoutSuccessMessage);
    }

    /// <summary>
    /// Writes the access token into an encrypted, HttpOnly cookie.
    /// Expiration is synchronized with JwtOptions.AccessTokenExpirationMinutes.
    /// </summary>
    private void SetAccessTokenCookie(string accessToken)
    {
        var cookieOptions = GetCookieOptions(DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes));
        Response.Cookies.Append(ApiConstants.AccessTokenCookieName, accessToken, cookieOptions);
    }

    /// <summary>
    /// Writes the refresh token into an encrypted, HttpOnly cookie.
    /// Expiration is synchronized with JwtOptions.RefreshTokenExpirationDays.
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = GetCookieOptions(DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays));
        Response.Cookies.Append(ApiConstants.RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    /// <summary>
    /// Standardized cookie options for HTTP / HTTPS localhost development and production environments.
    /// SameSite=Lax allows localhost same-site dev & protects against CSRF.
    /// </summary>
    private CookieOptions GetCookieOptions(DateTimeOffset expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = ApiConstants.AuthCookiePath,
            Expires = expires
        };
    }

    /// <summary>
    /// Clears both auth cookies upon logout or invalidation.
    /// </summary>
    private void ClearAuthCookies()
    {
        var expiredOptions = new CookieOptions
        {
            Path = ApiConstants.AuthCookiePath,
            Secure = Request.IsHttps,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UnixEpoch
        };

        Response.Cookies.Delete(ApiConstants.AccessTokenCookieName, expiredOptions);
        Response.Cookies.Delete(ApiConstants.RefreshTokenCookieName, expiredOptions);
    }
}
