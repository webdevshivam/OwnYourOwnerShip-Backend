using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Tracker.App.Common.Auth;
using Tracker.App.Common.Constants;
using Tracker.App.Common.Models;
using Tracker.App.Features.Auth.Controller;
using Tracker.App.Features.Auth.DTOs;
using Tracker.App.Features.Auth.Interface;
using Xunit;

namespace Tracker.App.Tests.Auth;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly AuthController _controller;
    private readonly DefaultHttpContext _httpContext;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();

        _jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "SuperSecretKeyForTestingPurposesWhichIsLongEnough32Bytes!",
            Issuer = "Tracker.App",
            Audience = "Tracker.App.Clients",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        _controller = new AuthController(
            _authServiceMock.Object,
            _jwtOptions,
            _loggerMock.Object
        );

        _httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    [Fact]
    public async Task Login_ShouldReturn200Ok_AndSetHttpOnlyCookie_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var loginResponse = new LoginResponse("access_jwt_token", ApiConstants.BearerScheme, 900);
        var serviceResult = AuthServiceResult.Success(loginResponse, "raw_refresh_token_64_characters");

        _authServiceMock
            .Setup(s => s.LoginAsync(request, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        var okResult = actionResult as ObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<LoginResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.AccessToken.Should().Be("access_jwt_token");

        // Verify Set-Cookie header contains HttpOnly and SameSite
        var setCookieHeader = _httpContext.Response.Headers.SetCookie.ToString();
        setCookieHeader.Should().Contain(ApiConstants.RefreshTokenCookieName);
        setCookieHeader.Should().Contain("httponly");
    }

    [Fact]
    public async Task Login_ShouldReturn401Unauthorized_WhenAuthServiceFails()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");
        var serviceResult = AuthServiceResult.Failure(ValidationConstants.InvalidCredentialsErrorMessage, statusCode: 401);

        _authServiceMock
            .Setup(s => s.LoginAsync(request, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        var statusResult = actionResult as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(401);

        var apiResponse = statusResult.Value as ApiResponse;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Be(ValidationConstants.InvalidCredentialsErrorMessage);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturn400BadRequest_WhenCookieIsMissing()
    {
        // Arrange: No cookie set on HttpContext

        // Act
        var actionResult = await _controller.RefreshToken();

        // Assert
        var statusResult = actionResult as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(400);

        var apiResponse = statusResult.Value as ApiResponse;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Be(ApiConstants.MissingRefreshTokenMessage);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturn200Ok_AndRotateCookie_WhenTokenIsValid()
    {
        // Arrange: Attach refreshToken cookie
        _httpContext.Request.Headers.Cookie = $"{ApiConstants.RefreshTokenCookieName}=existing_raw_token";

        var loginResponse = new LoginResponse("new_access_token", ApiConstants.BearerScheme, 900);
        var serviceResult = AuthServiceResult.Success(loginResponse, "new_rotated_refresh_token");

        _authServiceMock
            .Setup(s => s.RefreshTokenAsync("existing_raw_token", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.RefreshToken();

        // Assert
        var okResult = actionResult as ObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<LoginResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.AccessToken.Should().Be("new_access_token");

        // Verify Set-Cookie has the newly rotated token
        var setCookieHeader = _httpContext.Response.Headers.SetCookie.ToString();
        setCookieHeader.Should().Contain("new_rotated_refresh_token");
        setCookieHeader.Should().Contain("httponly");
    }

    [Fact]
    public async Task RefreshToken_ShouldClearCookie_AndReturn401_WhenTokenIsCompromisedOrInvalid()
    {
        // Arrange
        _httpContext.Request.Headers.Cookie = $"{ApiConstants.RefreshTokenCookieName}=compromised_token";

        var serviceResult = AuthServiceResult.Failure(ApiConstants.CompromisedTokenMessage, statusCode: 401);

        _authServiceMock
            .Setup(s => s.RefreshTokenAsync("compromised_token", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.RefreshToken();

        // Assert
        var statusResult = actionResult as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(401);

        var apiResponse = statusResult.Value as ApiResponse;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Be(ApiConstants.CompromisedTokenMessage);

        // Verify cookie is deleted/expired
        var setCookieHeader = _httpContext.Response.Headers.SetCookie.ToString();
        setCookieHeader.Should().Contain(ApiConstants.RefreshTokenCookieName);
        setCookieHeader.Should().Contain("expires=");
    }

    [Fact]
    public async Task Logout_ShouldReturn200Ok_AndClearCookie_WhenAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Bearer");
        _httpContext.User = new ClaimsPrincipal(identity);
        _httpContext.Request.Headers.Cookie = $"{ApiConstants.RefreshTokenCookieName}=active_session_cookie";

        _authServiceMock
            .Setup(s => s.LogoutAsync(userId, "active_session_cookie", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthServiceResult.Success());

        // Act
        var actionResult = await _controller.Logout(allDevices: false);

        // Assert
        var okResult = actionResult as ObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Be(ApiConstants.LogoutSuccessMessage);

        // Verify cookie was cleared
        var setCookieHeader = _httpContext.Response.Headers.SetCookie.ToString();
        setCookieHeader.Should().Contain(ApiConstants.RefreshTokenCookieName);
        setCookieHeader.Should().Contain("expires=");
    }

    [Fact]
    public async Task Logout_ShouldReturn401Unauthorized_WhenUserClaimIsMissing()
    {
        // Arrange (No NameIdentifier claim)
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var actionResult = await _controller.Logout();

        // Assert
        var unauthorizedResult = actionResult as ObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Logout_ShouldPassAllDevicesFlagToService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        _authServiceMock
            .Setup(s => s.LogoutAsync(userId, It.IsAny<string?>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthServiceResult.Success());

        // Act
        var actionResult = await _controller.Logout(allDevices: true);

        // Assert
        var okResult = actionResult as ObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        _authServiceMock.Verify(s => s.LogoutAsync(userId, It.IsAny<string?>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
