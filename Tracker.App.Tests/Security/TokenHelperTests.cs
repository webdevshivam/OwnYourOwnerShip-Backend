using FluentAssertions;
using Tracker.App.Common.Helpers;
using Xunit;

namespace Tracker.App.Tests.Security;

public class TokenHelperTests
{
    [Fact]
    public void GenerateRefreshToken_ShouldProduce64CharacterHexadecimalString()
    {
        // Act
        var token = TokenHelper.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        token.Length.Should().Be(64);
        token.Should().MatchRegex("^[0-9A-Fa-f]{64}$");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldProduceUniqueTokensEveryCall()
    {
        // Act
        var token1 = TokenHelper.GenerateRefreshToken();
        var token2 = TokenHelper.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void HashToken_ShouldBeDeterministicForSameInput()
    {
        // Arrange
        const string rawToken = "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789";

        // Act
        var hash1 = TokenHelper.HashToken(rawToken);
        var hash2 = TokenHelper.HashToken(rawToken);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(64);
    }

    [Fact]
    public void VerifyToken_ShouldReturnTrue_WhenTokensMatch()
    {
        // Arrange
        var rawToken = TokenHelper.GenerateRefreshToken();
        var storedHash = TokenHelper.HashToken(rawToken);

        // Act
        var result = TokenHelper.VerifyToken(rawToken, storedHash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyToken_ShouldReturnFalse_WhenTokensDoNotMatch()
    {
        // Arrange
        var rawToken = TokenHelper.GenerateRefreshToken();
        var differentToken = TokenHelper.GenerateRefreshToken();
        var storedHash = TokenHelper.HashToken(differentToken);

        // Act
        var result = TokenHelper.VerifyToken(rawToken, storedHash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HashToken_ShouldThrowArgumentException_WhenTokenIsWhitespace(string invalidToken)
    {
        // Act
        var act = () => TokenHelper.HashToken(invalidToken);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
