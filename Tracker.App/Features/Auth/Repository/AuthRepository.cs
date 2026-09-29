using Microsoft.EntityFrameworkCore;
using Tracker.App.Data;
using Tracker.App.Data.Entities;
using Tracker.App.Features.Auth.Interface;

namespace Tracker.App.Features.Auth.Repository;

/// <summary>
/// Entity Framework Core repository implementation for authentication data access.
/// Keeps database queries isolated from controller and service layers.
/// </summary>
public class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _context;

    public AuthRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // Direct equality comparison leverages PostgreSQL B-Tree unique index on users.email
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetRefreshTokenWithUserAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RotateRefreshTokenAsync(RefreshToken oldToken, RefreshToken newToken, CancellationToken cancellationToken = default)
    {
        // Mark old token as revoked
        oldToken.IsRevoked = true;
        oldToken.RevokedAt = DateTimeOffset.UtcNow;
        oldToken.ReplacedByToken = newToken;

        // Persist new token
        await _context.RefreshTokens.AddAsync(newToken, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        token.IsRevoked = true;
        token.RevokedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordFailedLoginAsync(
        Guid userId,
        int maxFailedAttempts,
        TimeSpan lockoutDuration,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.FailedLoginAttempts++;

        // Lock out account if maximum attempts exceeded
        if (user.FailedLoginAttempts >= maxFailedAttempts)
        {
            user.LockoutUntil = DateTimeOffset.UtcNow.Add(lockoutDuration);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordSuccessfulLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null)
        {
            return;
        }

        // Reset security counters
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
