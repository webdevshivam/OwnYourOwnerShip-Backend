using System.Security.Cryptography;
using System.Text;
using Tracker.App.Common.Constants;

namespace Tracker.App.Common.Helpers;

/// <summary>
/// Helper for generating cryptographically secure refresh tokens
/// and producing fast, deterministic SHA-256 hashes for database storage and indexed lookups.
/// </summary>
public static class TokenHelper
{
    /// <summary>
    /// Generates a cryptographically secure, random 64-character hexadecimal refresh token.
    /// This raw token is sent to the client application and never stored in the database directly.
    /// </summary>
    /// <returns>A 64-character random string with 256 bits of entropy.</returns>
    public static string GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(SecurityConstants.RefreshTokenByteSize);
        return Convert.ToHexString(randomBytes);
    }

    /// <summary>
    /// Computes the SHA-256 hash of a raw token.
    /// Deterministic: The same token will always produce the exact same 64-character hex hash,
    /// allowing instant indexed lookups in PostgreSQL: WHERE token_hash = @hash.
    /// </summary>
    /// <param name="rawToken">The raw refresh token provided by the client.</param>
    /// <returns>The 64-character hexadecimal SHA-256 hash to store or query in the database.</returns>
    /// <exception cref="ArgumentException">Thrown if rawToken is null, empty, or whitespace.</exception>
    public static string HashToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException(SecurityConstants.TokenEmptyErrorMessage, nameof(rawToken));
        }

        byte[] tokenBytes = Encoding.UTF8.GetBytes(rawToken);
        byte[] hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Verifies whether an incoming raw token matches a stored SHA-256 hash.
    /// Uses constant-time comparison (FixedTimeEquals) to eliminate timing attack risks.
    /// </summary>
    /// <param name="rawToken">The raw token received from the client.</param>
    /// <param name="storedHash">The stored SHA-256 hash from the database.</param>
    /// <returns>True if the token produces the stored hash; otherwise false.</returns>
    public static bool VerifyToken(string rawToken, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        // 1. Hash the incoming token
        string computedHash = HashToken(rawToken);

        // 2. Convert both hex strings to byte arrays for constant-time comparison
        byte[] computedBytes = Encoding.UTF8.GetBytes(computedHash);
        byte[] storedBytes = Encoding.UTF8.GetBytes(storedHash);

        // 3. Constant-time comparison
        return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
    }
}
