using Tracker.App.Data.Entities;

namespace Tracker.App.Features.Auth.Interface;

/// <summary>
/// Data persistence contract for authentication operations.
/// Encapsulates all database interactions for users, lockout metrics, and refresh tokens.
/// </summary>
public interface IAuthRepository
{
    /// <summary>
    /// Finds an active user by their email address.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new hashed refresh token record into database storage.
    /// </summary>
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a refresh token record along with its associated User entity by token hash.
    /// </summary>
    Task<RefreshToken?> GetRefreshTokenWithUserAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a refresh token by revoking the old token and persisting the new token in a single atomic transaction.
    /// </summary>
    Task RotateRefreshTokenAsync(RefreshToken oldToken, RefreshToken newToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active refresh tokens for a user when token theft or reuse is detected.
    /// </summary>
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed login attempt for brute-force tracking and locks the account if threshold is met.
    /// </summary>
    Task RecordFailedLoginAsync(Guid userId, int maxFailedAttempts, TimeSpan lockoutDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token by marking it revoked and setting RevokedAt timestamp.
    /// </summary>
    Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets failed login counters and updates the user's LastLoginAt timestamp upon successful authentication.
    /// </summary>
    Task RecordSuccessfulLoginAsync(Guid userId, CancellationToken cancellationToken = default);
}
