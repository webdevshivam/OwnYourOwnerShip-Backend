using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tracker.App.Common.Auth;
using Tracker.App.Common.Helpers;
using Tracker.App.Data.Entities;
using Tracker.App.Features.Auth.Interface;

namespace Tracker.App.Features.Auth.Service;

/// <summary>
/// Service responsible for minting short-lived JWT Access Tokens
/// and generating high-entropy, hashed Refresh Tokens.
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    /// <inheritdoc />
    public (string RawToken, RefreshToken Entity) CreateRefreshToken(Guid userId, string? ipAddress, string? deviceInfo)
    {
        // 1. Generate 256 bits of cryptographically secure random bytes (64 hex characters)
        string rawToken = TokenHelper.GenerateRefreshToken();

        // 2. Compute deterministic SHA-256 hash for database storage (never store raw tokens)
        string tokenHash = TokenHelper.HashToken(rawToken);

        // 3. Build persistent entity
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo
        };

        return (rawToken, entity);
    }

    /// <inheritdoc />
    public int GetAccessTokenExpirationSeconds()
    {
        return _jwtOptions.AccessTokenExpirationMinutes * 60;
    }
}
