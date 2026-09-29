using Microsoft.Extensions.Logging;
using Tracker.App.Common.Constants;
using Tracker.App.Common.Helpers;
using Tracker.App.Features.Auth.DTOs;
using Tracker.App.Features.Auth.Interface;

namespace Tracker.App.Features.Auth.Service;

/// <summary>
/// Domain service coordinating authentication business logic:
/// - Email lookup
/// - Brute-force lockout enforcement
/// - Constant-time Argon2id password verification
/// - JWT access token & hashed refresh token issuance
/// - Refresh token rotation & reuse detection (theft defense)
/// </summary>
public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Dummy Argon2id hash used to equalize execution timing when email does not exist (prevents user enumeration)
    private static readonly string DummyPasswordHash =
        PasswordHashHelper.HashPassword("DummyConstantPassword123!");

    private readonly IAuthRepository _authRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthRepository authRepository,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthServiceResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        // ---------------------------------------------------------------------
        // 1. User Lookup
        // ---------------------------------------------------------------------
        var user = await _authRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Login failed: email '{Email}' not found. Origin IP: {IpAddress}", request.Email, ipAddress);

            // Execute dummy Argon2id verification to equalize response timing and prevent user enumeration
            PasswordHashHelper.VerifyPassword(request.Password, DummyPasswordHash);

            return AuthServiceResult.Failure(ValidationConstants.InvalidCredentialsErrorMessage, statusCode: 401);
        }

        // ---------------------------------------------------------------------
        // 2. Lockout Check (Brute-Force Defense)
        // ---------------------------------------------------------------------
        if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTimeOffset.UtcNow)
        {
            var remainingMinutes = Math.Ceiling((user.LockoutUntil.Value - DateTimeOffset.UtcNow).TotalMinutes);
            _logger.LogWarning("Login rejected: user '{UserId}' is locked out for {RemainingMinutes} more minutes.", user.Id, remainingMinutes);
            return AuthServiceResult.Failure(
                $"{ValidationConstants.AccountLockedErrorMessage} Try again in {remainingMinutes} minutes.",
                statusCode: 423 // 423 Locked
            );
        }

        // ---------------------------------------------------------------------
        // 3. Account Status Check
        // ---------------------------------------------------------------------
        if (!user.IsActive)
        {
            _logger.LogWarning("Login rejected: user account '{UserId}' is inactive.", user.Id);
            return AuthServiceResult.Failure(ValidationConstants.AccountInactiveErrorMessage, statusCode: 403);
        }

        // ---------------------------------------------------------------------
        // 4. Password Verification (Argon2id with Constant-Time Equality)
        // ---------------------------------------------------------------------
        bool isPasswordValid = PasswordHashHelper.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: invalid password for user '{UserId}'. Origin IP: {IpAddress}", user.Id, ipAddress);

            // Record failed attempt and apply lockout if threshold reached
            await _authRepository.RecordFailedLoginAsync(user.Id, MaxFailedAttempts, LockoutDuration, cancellationToken);

            return AuthServiceResult.Failure(ValidationConstants.InvalidCredentialsErrorMessage, statusCode: 401);
        }

        // ---------------------------------------------------------------------
        // 5. Successful Authentication: Reset Lockout & Record Activity
        // ---------------------------------------------------------------------
        await _authRepository.RecordSuccessfulLoginAsync(user.Id, cancellationToken);
        _logger.LogInformation("User '{UserId}' successfully authenticated from IP: {IpAddress}", user.Id, ipAddress);

        // ---------------------------------------------------------------------
        // 6. Token Issuance (Short-lived JWT + Hashed Refresh Token)
        // ---------------------------------------------------------------------
        string accessToken = _tokenService.GenerateAccessToken(user);
        var (rawRefreshToken, refreshTokenEntity) = _tokenService.CreateRefreshToken(user.Id, ipAddress, deviceInfo);

        // Persist hashed refresh token to database
        await _authRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);

        // ---------------------------------------------------------------------
        // 7. Assemble Lightweight Response Payload
        // ---------------------------------------------------------------------
        var response = new LoginResponse(
            AccessToken: accessToken,
            TokenType: ApiConstants.BearerScheme,
            ExpiresInSeconds: _tokenService.GetAccessTokenExpirationSeconds()
        );

        return AuthServiceResult.Success(response, rawRefreshToken);
    }

    /// <inheritdoc />
    public async Task<AuthServiceResult> RefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        // ---------------------------------------------------------------------
        // 1. Hash Incoming Raw Token for Deterministic Indexed DB Lookup
        // ---------------------------------------------------------------------
        string tokenHash;
        try
        {
            tokenHash = TokenHelper.HashToken(rawRefreshToken);
        }
        catch (ArgumentException)
        {
            return AuthServiceResult.Failure(ApiConstants.InvalidRefreshTokenMessage, statusCode: 401);
        }

        var existingToken = await _authRepository.GetRefreshTokenWithUserAsync(tokenHash, cancellationToken);
        if (existingToken is null)
        {
            _logger.LogWarning("Refresh token lookup failed: hash not found. Origin IP: {IpAddress}", ipAddress);
            return AuthServiceResult.Failure(ApiConstants.InvalidRefreshTokenMessage, statusCode: 401);
        }

        // ---------------------------------------------------------------------
        // 2. THEFT DEFENSE (Reuse Detection): Old Revoked Token Presented!
        // ---------------------------------------------------------------------
        if (existingToken.IsRevoked)
        {
            _logger.LogWarning("SECURITY ALERT: Revoked refresh token reused by User '{UserId}'. Terminating all sessions! Origin IP: {IpAddress}", existingToken.UserId, ipAddress);

            // Invalidate the entire token family for this user
            await _authRepository.RevokeAllUserTokensAsync(existingToken.UserId, cancellationToken);

            return AuthServiceResult.Failure(ApiConstants.CompromisedTokenMessage, statusCode: 401);
        }

        // ---------------------------------------------------------------------
        // 3. Expiration Check
        // ---------------------------------------------------------------------
        if (existingToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Refresh token expired for User '{UserId}'. Origin IP: {IpAddress}", existingToken.UserId, ipAddress);
            return AuthServiceResult.Failure(ApiConstants.InvalidRefreshTokenMessage, statusCode: 401);
        }

        // ---------------------------------------------------------------------
        // 4. User Status Check
        // ---------------------------------------------------------------------
        if (!existingToken.User.IsActive)
        {
            _logger.LogWarning("Refresh token rejected: User account '{UserId}' is inactive.", existingToken.UserId);
            return AuthServiceResult.Failure(ValidationConstants.AccountInactiveErrorMessage, statusCode: 403);
        }

        // ---------------------------------------------------------------------
        // 5. Token Rotation: Mint New Access Token & New Refresh Token
        // ---------------------------------------------------------------------
        string newAccessToken = _tokenService.GenerateAccessToken(existingToken.User);
        var (newRawRefreshToken, newRefreshTokenEntity) = _tokenService.CreateRefreshToken(existingToken.UserId, ipAddress, deviceInfo);

        // Atomically revoke old token and persist the new token
        await _authRepository.RotateRefreshTokenAsync(existingToken, newRefreshTokenEntity, cancellationToken);
        _logger.LogInformation("Refresh token successfully rotated for User '{UserId}'. Origin IP: {IpAddress}", existingToken.UserId, ipAddress);

        // ---------------------------------------------------------------------
        // 6. Return Clean Response Payload
        // ---------------------------------------------------------------------
        var response = new LoginResponse(
            AccessToken: newAccessToken,
            TokenType: ApiConstants.BearerScheme,
            ExpiresInSeconds: _tokenService.GetAccessTokenExpirationSeconds()
        );

        return AuthServiceResult.Success(response, newRawRefreshToken);
    }

    /// <inheritdoc />
    public async Task<AuthServiceResult> LogoutAsync(
        Guid userId,
        string? rawRefreshToken,
        bool allDevices = false,
        CancellationToken cancellationToken = default)
    {
        // 1. If global logout is requested, revoke all active sessions for this user
        if (allDevices)
        {
            await _authRepository.RevokeAllUserTokensAsync(userId, cancellationToken);
            _logger.LogInformation("All sessions successfully revoked for User '{UserId}' upon global logout.", userId);
            return AuthServiceResult.Success();
        }

        // 2. Single-session logout: revoke the specific session identified by the refresh token
        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            string tokenHash;
            try
            {
                tokenHash = TokenHelper.HashToken(rawRefreshToken);
            }
            catch (ArgumentException)
            {
                // Malformed raw token - ignore and succeed since client cookie is cleared anyway
                return AuthServiceResult.Success();
            }

            var token = await _authRepository.GetRefreshTokenWithUserAsync(tokenHash, cancellationToken);

            // BOLA / IDOR Defense: Only revoke if the token strictly belongs to the authenticated user
            if (token is not null && token.UserId == userId && !token.IsRevoked)
            {
                await _authRepository.RevokeRefreshTokenAsync(token, cancellationToken);
                _logger.LogInformation("Session refresh token revoked for User '{UserId}'.", userId);
            }
        }

        return AuthServiceResult.Success();
    }
}
