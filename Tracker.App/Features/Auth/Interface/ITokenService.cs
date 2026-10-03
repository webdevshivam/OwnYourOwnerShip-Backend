using Tracker.App.Data.Entities;

namespace Tracker.App.Features.Auth.Interface;

/// <summary>
/// Contract for cryptographically secure token generation and management.
/// Handles JWT Access Token creation and Refresh Token issuance with SHA-256 hashing.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed, short-lived JWT Access Token containing user claims.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically random raw refresh token for the client,
    /// along with a corresponding hashed RefreshToken entity for database persistence.
    /// </summary>
    /// <param name="userId">The user ID owning the token.</param>
    /// <param name="ipAddress">Client IP address originating the request.</param>
    /// <param name="deviceInfo">Client User-Agent / device signature.</param>
    /// <returns>Tuple containing (rawTokenToSendToClient, entityToSaveInDatabase).</returns>
    (string RawToken, RefreshToken Entity) CreateRefreshToken(Guid userId, string? ipAddress, string? deviceInfo);

    /// <summary>
    /// Returns the configured lifespan of access tokens in seconds.
    /// </summary>
    int GetAccessTokenExpirationSeconds();
}
