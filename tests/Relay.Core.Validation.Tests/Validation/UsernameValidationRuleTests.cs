using System.Linq;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

namespace Relay.Core.Tests.Validation;

public class UsernameValidationRuleTests
{
    private readonly UsernameValidationRule _rule = new();

    [Theory]
    [InlineData("user123")]
    [InlineData("test_user")]
    [InlineData("john-doe")]
    [InlineData("user.name")]
    [InlineData("a1b2c3")]
    [InlineData("my_username_123")]
    [InlineData("john.doe-smith")]
    [InlineData("abc")] // Minimum length
    [InlineData("abcdefghijklmnopqrstuvwxyz123456")] // Maximum length (32 chars)
    public async Task ValidateAsync_ValidUsernames_ReturnsEmptyErrors(string username)
    {
        // Act
        var result = await _rule.ValidateAsync(username);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("ab")] // Too short
    [InlineData("a")] // Too short
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_TooShortUsernames_ReturnsError(string username)
    {
        // Act
        var result = await _rule.ValidateAsync(username);

        // Assert
        if (string.IsNullOrWhiteSpace(username))
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Username must be at least 3 characters long.", result.First());
        }
    }

    [Fact]
    public async Task ValidateAsync_TooLongUsername_ReturnsError()
    {
        // Arrange
        var longUsername = new string('a', 33); // 33 characters

        // Act
        var result = await _rule.ValidateAsync(longUsername);

        // Assert
        Assert.Single(result);
        Assert.Equal("Username cannot exceed 32 characters.", result.First());
    }

    [Theory]
    [InlineData("_username")] // Starts with underscore
    [InlineData("-username")] // Starts with hyphen
    [InlineData(".username")] // Starts with dot
    [InlineData("username_")] // Ends with underscore
    [InlineData("username-")] // Ends with hyphen
    [InlineData("username.")] // Ends with dot
    [InlineData("user name")] // Space
    [InlineData("user@name")] // Invalid character
    [InlineData("user!name")] // Invalid character
    public async Task ValidateAsync_InvalidFormatUsernames_ReturnsError(string username)
    {
        // Act
        var result = await _rule.ValidateAsync(username);

        // Assert
        Assert.Single(result);
        Assert.Contains("Username can only contain letters, numbers, underscores, hyphens, and dots", result.First());
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("root")]
    [InlineData("system")]
    [InlineData("guest")]
    [InlineData("user")]
    [InlineData("test")]
    [InlineData("null")]
    [InlineData("undefined")]
    [InlineData("api")]
    [InlineData("www")]
    [InlineData("mail")]
    [InlineData("ftp")]
    [InlineData("localhost")]
    [InlineData("ADMIN")] // Case insensitive
    [InlineData("Root")]
    public async Task ValidateAsync_ReservedUsernames_ReturnsError(string username)
    {
        // Act
        var result = await _rule.ValidateAsync(username);

        // Assert
        Assert.Single(result);
        Assert.Equal("This username is reserved and cannot be used.", result.First());
    }

    [Theory]
    [InlineData("user..name")] // Consecutive dots
    [InlineData("user__name")] // Consecutive underscores
    [InlineData("user--name")] // Consecutive hyphens
    [InlineData("user._name")] // Mixed consecutive
    public async Task ValidateAsync_ConsecutiveSpecialChars_ReturnsError(string username)
    {
        // Act
        var result = await _rule.ValidateAsync(username);

        // Assert
        Assert.Single(result);
        Assert.Equal("Username cannot contain consecutive special characters.", result.First());
    }

    [Theory]
    [InlineData("user.name")] // Valid dot
    [InlineData("user_name")] // Valid underscore
    [InlineData("user-name")] // Valid hyphen
    [InlineData("user.name_123-test")] // Valid mixed
    public async Task ValidateAsync_ValidSpecialChars_ReturnsEmptyErrors(string username)
    {
        // Act
        var result = await _rule.ValidateAsync(username);

        // Assert
        Assert.Empty(result);
    }
}