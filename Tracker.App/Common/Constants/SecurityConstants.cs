namespace Tracker.App.Common.Constants;

/// <summary>
/// Centralized cryptographic, hashing, and security constants.
/// Adheres strictly to OWASP guidelines for Argon2id and SHA-256 hashing.
/// </summary>
public static class SecurityConstants
{
    // =========================================================================
    // Argon2id Cryptographic Parameters (For Passwords)
    // =========================================================================

    /// <summary>
    /// 128-bit cryptographically secure random salt size (in bytes).
    /// </summary>
    public const int SaltByteSize = 16;

    /// <summary>
    /// 256-bit derived key / hash length (in bytes).
    /// </summary>
    public const int HashByteSize = 32;

    /// <summary>
    /// Time cost: Number of passes the algorithm performs over memory.
    /// </summary>
    public const int Argon2Iterations = 3;

    /// <summary>
    /// Memory cost: 64 MB (65,536 KB).
    /// Forces attackers with GPUs/ASICs to dedicate significant RAM per crack attempt.
    /// </summary>
    public const int Argon2MemorySizeInKb = 65536;

    /// <summary>
    /// Degree of parallelism: Number of computational threads/lanes.
    /// </summary>
    public const int Argon2DegreeOfParallelism = 4;

    /// <summary>
    /// Delimiter separating Salt and Hash in the stored string: "{Salt}:{Hash}".
    /// </summary>
    public const char HashDelimiter = ':';

    // =========================================================================
    // Refresh Token Parameters (SHA-256)
    // =========================================================================

    /// <summary>
    /// 256 bits of cryptographically secure randomness (32 bytes = 64 hex characters).
    /// </summary>
    public const int RefreshTokenByteSize = 32;

    /// <summary>
    /// Length of a SHA-256 hexadecimal output string.
    /// </summary>
    public const int Sha256HexLength = 64;

    // =========================================================================
    // Error Messages & Text
    // =========================================================================

    public const string PasswordEmptyErrorMessage = "Password cannot be null, empty, or whitespace.";
    public const string HashEmptyErrorMessage = "Stored hash cannot be null, empty, or whitespace.";
    public const string MalformedHashFormatErrorMessage = "Stored password hash format is invalid.";
    public const string TokenEmptyErrorMessage = "Token cannot be null, empty, or whitespace.";
}
