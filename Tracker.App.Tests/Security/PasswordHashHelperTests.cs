using FluentAssertions;
using Tracker.App.Common.Helpers;
using Xunit;

namespace Tracker.App.Tests.Security;

public class PasswordHashHelperTests
{
    [Fact]
    public void HashPassword_ShouldReturnValidSaltAndHashFormat()
    {
        // Arrange
        const string password = "StrongPassword123!";

        // Act
        var hash = PasswordHashHelper.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().Contain(":");
        var parts = hash.Split(':');
        parts.Length.Should().Be(2);
        parts[0].Should().NotBeEmpty(); // Base64 Salt
        parts[1].Should().NotBeEmpty(); // Base64 Hash
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_ForCorrectPassword()
    {
        // Arrange
        const string password = "CorrectPassword123!";
        var hash = PasswordHashHelper.HashPassword(password);

        // Act
        var isValid = PasswordHashHelper.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_ForIncorrectPassword()
    {
        // Arrange
        var hash = PasswordHashHelper.HashPassword("CorrectPassword123!");

        // Act
        var isValid = PasswordHashHelper.VerifyPassword("WrongPassword123!", hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyPassword_ShouldReturnFalse_ForEmptyOrWhitespacePassword(string invalidPassword)
    {
        // Arrange
        var hash = PasswordHashHelper.HashPassword("ValidPassword123!");

        // Act
        var isValid = PasswordHashHelper.VerifyPassword(invalidPassword, hash);

        // Assert
        isValid.Should().BeFalse();
    }
}
