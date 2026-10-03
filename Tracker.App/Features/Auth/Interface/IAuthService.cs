using Tracker.App.Features.Auth.DTOs;

namespace Tracker.App.Features.Auth.Interface;

/// <summary>
/// Business logic orchestration contract for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates user credentials, enforces brute-force protection,
    /// verifies Argon2 password hashes, and issues access + refresh tokens.
    /// </summary>
    Task<AuthServiceResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an incoming refresh token, enforces reuse detection (theft defense),
    /// rotates the refresh token, and issues a new access token.
    /// </summary>
    Task<AuthServiceResult> RefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out an authenticated user by invalidating the active session's refresh token
    /// or all active refresh tokens if requested.
    /// </summary>
    Task<AuthServiceResult> LogoutAsync(
        Guid userId,
        string? rawRefreshToken,
        bool allDevices = false,
        CancellationToken cancellationToken = default);
}
