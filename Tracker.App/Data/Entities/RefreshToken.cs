using Tracker.App.Common.Entities;

namespace Tracker.App.Data.Entities;

/// <summary>
/// Stores hashed refresh tokens for multi-device authentication and session rotation.
/// Raw tokens are never stored in the database.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// SHA-256 hash of the cryptographically random token string.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsRevoked { get; set; } = false;
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// For Token Rotation (RTR): References the new token that replaced this one.
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }

    // Client metadata
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }

    /// <summary>
    /// Helper property to check if the token is currently valid.
    /// </summary>
    public bool IsActive => !IsRevoked && DateTimeOffset.UtcNow < ExpiresAt;
}
