using Moq;
using Relay.Core.Validation.Rules;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class UniqueUsernameValidationRuleTests
{
    [Fact]
    public void Constructor_Should_Throw_When_UniquenessChecker_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UniqueUsernameValidationRule(null));
    }

    [Fact]
    public async Task ValidateAsync_WithNull_Username_Returns_Empty_Errors()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync(null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Empty_Username_Returns_Empty_Errors()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Whitespace_Username_Returns_Empty_Errors()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("   ");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Unique_Username_Returns_Empty_Errors()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("unique_user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("unique_user");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Non_Unique_Username_Returns_Error()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("taken_user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("taken_user");

        // Assert
        Assert.Single(result);
        Assert.Equal("Username is already taken. Please choose a different username.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_With_Exception_Thrown_Returns_Error()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("test_user", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));
        
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("test_user");

        // Assert
        Assert.Single(result);
        Assert.Equal("Unable to verify username uniqueness. Please try again later.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_Cancellation_Token_Called()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        var cts = new CancellationTokenSource();
        
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("user", cts.Token))
            .ReturnsAsync(true);
        
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("user", cts.Token);

        // Assert
        Assert.Empty(result);
        mockChecker.Verify(x => x.IsUsernameUniqueAsync("user", cts.Token), Times.Once);
    }



    [Theory]
    [InlineData("valid_user1")]
    [InlineData("user_name")]
    [InlineData("user.name")]
    [InlineData("user-name")]
    [InlineData("A1B2C3")]
    public async Task ValidateAsync_With_Valid_Usernames_Returns_Empty_Or_Errors_Based_On_Uniqueness(string username)
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        // Setup mock to return true (unique) for this test
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync(username);

        // Assert - Should be empty if username is unique
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Long_Username()
    {
        // Arrange
        var longUsername = new string('a', 100);
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync(longUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync(longUsername);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_With_Different_Exception_Types()
    {
        // Arrange
        var mockChecker = new Mock<IUsernameUniquenessChecker>();
        mockChecker
            .Setup(x => x.IsUsernameUniqueAsync("user", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));
        
        var rule = new UniqueUsernameValidationRule(mockChecker.Object);

        // Act
        var result = await rule.ValidateAsync("user");

        // Assert
        Assert.Single(result);
        Assert.Equal("Unable to verify username uniqueness. Please try again later.", result.First());
    }
}