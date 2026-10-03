using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Tracker.App.Common.Auth;
using Tracker.App.Common.Constants;
using Tracker.App.Common.Helpers;
using Tracker.App.Data.Entities;
using Tracker.App.Features.Auth.DTOs;
using Tracker.App.Features.Auth.Interface;
using Tracker.App.Features.Auth.Service;
using Xunit;

namespace Tracker.App.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _authRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authRepositoryMock = new Mock<IAuthRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _authRepositoryMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object
        );
    }

    // =========================================================================
    // LOGIN TESTS
    // =========================================================================

    [Fact]
    public async Task LoginAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        const string rawPassword = "ValidPassword123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = PasswordHashHelper.HashPassword(rawPassword),
            FullName = "Jane Doe",
            IsActive = true
        };

        _authRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(user))
            .Returns("mock.jwt.token");

        var dummyRefreshTokenEntity = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id };
        _tokenServiceMock
            .Setup(t => t.CreateRefreshToken(user.Id, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("raw_refresh_token_64chars", dummyRefreshTokenEntity));

        _tokenServiceMock
            .Setup(t => t.GetAccessTokenExpirationSeconds())
            .Returns(900);

        var request = new LoginRequest(user.Email, rawPassword);

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1", "Mozilla/5.0");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.User.Email.Should().Be(user.Email);
        result.Data.User.FullName.Should().Be(user.FullName);
        result.Data.TokenType.Should().Be(ApiConstants.BearerScheme);
        result.Data.ExpiresInSeconds.Should().Be(900);
        result.AccessToken.Should().Be("mock.jwt.token");
        result.RefreshToken.Should().Be("raw_refresh_token_64chars");

        _authRepositoryMock.Verify(r => r.RecordSuccessfulLoginAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _authRepositoryMock.Verify(r => r.AddRefreshTokenAsync(dummyRefreshTokenEntity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnUnauthorized_WhenEmailNotFound()
    {
        // Arrange
        _authRepositoryMock
            .Setup(r => r.GetByEmailAsync("unknown@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = new LoginRequest("unknown@example.com", "SomePassword123!");

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1", "curl");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.ErrorMessage.Should().Be(ValidationConstants.InvalidCredentialsErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnUnauthorized_AndIncrementFailures_WhenPasswordIsIncorrect()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = PasswordHashHelper.HashPassword("CorrectPassword123!"),
            IsActive = true
        };

        _authRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new LoginRequest(user.Email, "WrongPassword999!");

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1", "curl");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.ErrorMessage.Should().Be(ValidationConstants.InvalidCredentialsErrorMessage);

        _authRepositoryMock.Verify(r => r.RecordFailedLoginAsync(user.Id, 5, TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnLocked_WhenUserAccountIsLockedOut()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "locked@example.com",
            PasswordHash = PasswordHashHelper.HashPassword("Password123!"),
            IsActive = true,
            LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(12)
        };

        _authRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new LoginRequest(user.Email, "Password123!");

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1", "curl");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(423); // 423 Locked
        result.ErrorMessage.Should().Contain(ValidationConstants.AccountLockedErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnForbidden_WhenUserIsDeactivated()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "banned@example.com",
            PasswordHash = PasswordHashHelper.HashPassword("Password123!"),
            IsActive = false
        };

        _authRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new LoginRequest(user.Email, "Password123!");

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1", "curl");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403); // 403 Forbidden
        result.ErrorMessage.Should().Be(ValidationConstants.AccountInactiveErrorMessage);
    }

    // =========================================================================
    // REFRESH TOKEN ROTATION & THEFT DETECTION TESTS
    // =========================================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldRotateTokens_WhenTokenIsValid()
    {
        // Arrange
        const string rawRefreshToken = "valid_raw_refresh_token_string_here_1234567890";
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", IsActive = true };
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TokenHash = TokenHelper.HashToken(rawRefreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _authRepositoryMock
            .Setup(r => r.GetRefreshTokenWithUserAsync(existingToken.TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(user))
            .Returns("new.jwt.access.token");

        var newRefreshTokenEntity = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id };
        _tokenServiceMock
            .Setup(t => t.CreateRefreshToken(user.Id, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("new_rotated_raw_token", newRefreshTokenEntity));

        _tokenServiceMock
            .Setup(t => t.GetAccessTokenExpirationSeconds())
            .Returns(900);

        // Act
        var result = await _authService.RefreshTokenAsync(rawRefreshToken, "127.0.0.1", "Firefox");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.User.Email.Should().Be(user.Email);
        result.AccessToken.Should().Be("new.jwt.access.token");
        result.RefreshToken.Should().Be("new_rotated_raw_token");

        _authRepositoryMock.Verify(r => r.RotateRefreshTokenAsync(existingToken, newRefreshTokenEntity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldDetectReuse_AndRevokeAllSessions_WhenRevokedTokenIsReplayed()
    {
        // Arrange (Simulate stolen token that was already rotated)
        const string stolenRawToken = "stolen_token_previously_rotated";
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", IsActive = true };
        var compromisedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TokenHash = TokenHelper.HashToken(stolenRawToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = true, // Intruder presents an already spent token!
            RevokedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        _authRepositoryMock
            .Setup(r => r.GetRefreshTokenWithUserAsync(compromisedToken.TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compromisedToken);

        // Act
        var result = await _authService.RefreshTokenAsync(stolenRawToken, "192.168.1.100", "AttackerBot");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.ErrorMessage.Should().Be(ApiConstants.CompromisedTokenMessage);

        // Security check: Must have terminated ALL active sessions for that user
        _authRepositoryMock.Verify(r => r.RevokeAllUserTokensAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnUnauthorized_WhenTokenIsExpired()
    {
        // Arrange
        const string expiredRawToken = "expired_raw_token";
        var user = new User { Id = Guid.NewGuid(), IsActive = true };
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TokenHash = TokenHelper.HashToken(expiredRawToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10), // Expired!
            IsRevoked = false
        };

        _authRepositoryMock
            .Setup(r => r.GetRefreshTokenWithUserAsync(expiredToken.TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        // Act
        var result = await _authService.RefreshTokenAsync(expiredRawToken, "127.0.0.1", "curl");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.ErrorMessage.Should().Be(ApiConstants.InvalidRefreshTokenMessage);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeCurrentSession_WhenMatchingTokenProvided()
    {
        // Arrange
        const string rawRefreshToken = "valid_user_session_token";
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@example.com" };
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = user,
            TokenHash = TokenHelper.HashToken(rawRefreshToken),
            IsRevoked = false
        };

        _authRepositoryMock
            .Setup(r => r.GetRefreshTokenWithUserAsync(token.TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _authService.LogoutAsync(userId, rawRefreshToken, allDevices: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _authRepositoryMock.Verify(r => r.RevokeRefreshTokenAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _authRepositoryMock.Verify(r => r.RevokeAllUserTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeAllUserTokens_WhenAllDevicesIsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _authService.LogoutAsync(userId, "any_token", allDevices: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _authRepositoryMock.Verify(r => r.RevokeAllUserTokensAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _authRepositoryMock.Verify(r => r.RevokeRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRevokeToken_WhenTokenBelongsToDifferentUser()
    {
        // Arrange (IDOR defense)
        const string rawRefreshToken = "attacker_provided_token";
        var authenticatedUserId = Guid.NewGuid();
        var victimUserId = Guid.NewGuid();

        var victimToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = victimUserId, // Belongs to someone else!
            TokenHash = TokenHelper.HashToken(rawRefreshToken),
            IsRevoked = false
        };

        _authRepositoryMock
            .Setup(r => r.GetRefreshTokenWithUserAsync(victimToken.TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(victimToken);

        // Act
        var result = await _authService.LogoutAsync(authenticatedUserId, rawRefreshToken, allDevices: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Crucial security invariant: token of victim is NOT revoked
        _authRepositoryMock.Verify(r => r.RevokeRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
