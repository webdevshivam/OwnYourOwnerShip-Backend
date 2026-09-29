using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.App.Data;
using Tracker.App.Data.Entities;
using Tracker.App.Features.Auth.Repository;
using Xunit;

namespace Tracker.App.Tests.Auth;

public class AuthRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuthRepository _repository;

    public AuthRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new AuthRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldFindUser_RegardlessOfEmailCasing()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "john.doe@example.com",
            PasswordHash = "hash123",
            FullName = "John Doe"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var foundUser = await _repository.GetByEmailAsync("JOHN.DOE@EXAMPLE.COM");

        // Assert
        foundUser.Should().NotBeNull();
        foundUser!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task RecordFailedLoginAsync_ShouldLockUser_WhenThresholdIsReached()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "target@example.com",
            PasswordHash = "hash123",
            FullName = "Target User",
            FailedLoginAttempts = 4
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act (5th attempt hits threshold of 5)
        await _repository.RecordFailedLoginAsync(user.Id, maxFailedAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FailedLoginAttempts.Should().Be(5);
        updatedUser.LockoutUntil.Should().NotBeNull();
        updatedUser.LockoutUntil!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task RecordSuccessfulLoginAsync_ShouldResetCountersAndClearLockout()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "recovering@example.com",
            PasswordHash = "hash123",
            FullName = "Recovering User",
            FailedLoginAttempts = 3,
            LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RecordSuccessfulLoginAsync(user.Id);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(0);
        updatedUser.LockoutUntil.Should().BeNull();
        updatedUser.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldMarkAllActiveTokensAsRevoked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token1 = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "hash1", IsRevoked = false };
        var token2 = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "hash2", IsRevoked = false };
        var otherUserToken = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TokenHash = "hash3", IsRevoked = false };

        await _context.RefreshTokens.AddRangeAsync(token1, token2, otherUserToken);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RevokeAllUserTokensAsync(userId);

        // Assert
        var tokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        tokens.Should().HaveCount(2);
        tokens.Should().OnlyContain(t => t.IsRevoked && t.RevokedAt != null);

        var unaffectedToken = await _context.RefreshTokens.FindAsync(otherUserToken.Id);
        unaffectedToken!.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldSetIsRevokedAndRevokedAt()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "unique_hash_for_revocation",
            IsRevoked = false
        };
        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RevokeRefreshTokenAsync(token);

        // Assert
        var updated = await _context.RefreshTokens.FindAsync(token.Id);
        updated!.IsRevoked.Should().BeTrue();
        updated.RevokedAt.Should().NotBeNull();
        updated.RevokedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }
}
