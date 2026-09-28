using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Tracker.App.Common.Constants;

namespace Tracker.App.Common.Helpers;

/// <summary>
/// Production-grade password hashing and verification helper using Argon2id.
/// Uses centralized constants from SecurityConstants to avoid magic numbers and strings.
/// </summary>
public static class PasswordHashHelper
{
    /// <summary>
    /// Hashes a plaintext password using Argon2id with a unique, cryptographically random salt.
    /// Returns the combined string in format: "{Base64(salt)}:{Base64(hash)}".
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>A secure, formatted hash string ready to be stored in the database.</returns>
    /// <exception cref="ArgumentException">Thrown if password is null, empty, or whitespace.</exception>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(SecurityConstants.PasswordEmptyErrorMessage, nameof(password));
        }

        // 1. Generate a cryptographically secure random salt
        byte[] salt = RandomNumberGenerator.GetBytes(SecurityConstants.SaltByteSize);

        // 2. Hash the password using Argon2id with recommended parameters
        byte[] hash = GenerateArgon2Hash(password, salt);

        // 3. Convert bytes to Base64 strings
        string saltBase64 = Convert.ToBase64String(salt);
        string hashBase64 = Convert.ToBase64String(hash);

        // 4. Return combined string using centralized delimiter
        return $"{saltBase64}{SecurityConstants.HashDelimiter}{hashBase64}";
    }

    /// <summary>
    /// Verifies whether an incoming plaintext password matches the stored Argon2id hash.
    /// Uses constant-time comparison (FixedTimeEquals) to prevent timing attacks.
    /// </summary>
    /// <param name="password">The plaintext password provided by the user during login.</param>
    /// <param name="storedHash">The stored hash string from the database.</param>
    /// <returns>True if the password matches; otherwise false.</returns>
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        // 1. Split the stored hash into Salt and Hash components using the delimiter
        string[] parts = storedHash.Split(SecurityConstants.HashDelimiter);
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);

            // 2. Defensive validation: verify lengths match security specifications
            if (salt.Length != SecurityConstants.SaltByteSize || expectedHash.Length != SecurityConstants.HashByteSize)
            {
                return false;
            }

            // 3. Compute hash for the incoming password using the extracted salt
            byte[] actualHash = GenerateArgon2Hash(password, salt);

            // 4. Compare in constant time to prevent side-channel timing attacks
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            // Invalid Base64 characters in stored hash
            return false;
        }
    }

    /// <summary>
    /// Internal execution of the Argon2id hashing algorithm.
    /// </summary>
    private static byte[] GenerateArgon2Hash(string password, byte[] salt)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        using var argon2 = new Argon2id(passwordBytes)
        {
            Salt = salt,
            DegreeOfParallelism = SecurityConstants.Argon2DegreeOfParallelism,
            MemorySize = SecurityConstants.Argon2MemorySizeInKb,
            Iterations = SecurityConstants.Argon2Iterations
        };

        return argon2.GetBytes(SecurityConstants.HashByteSize);
    }
}
