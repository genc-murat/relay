using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class MinLengthValidationRuleTests
{
    [Theory]
    [InlineData("hello", 3)] // Well above min
    [InlineData("test", 4)] // Exactly at min
    [InlineData("", 0)] // Empty string at min 0
    [InlineData("a", 1)] // Single char at min
    [InlineData("hello world", 5)] // With spaces
    [InlineData("1234567890", 10)] // Numbers
    [InlineData("!@#$%^&*()", 10)] // Special chars
    public async Task ValidateAsync_ValidLengths_ReturnsEmptyErrors(string input, int minLength)
    {
        // Arrange
        var rule = new MinLengthValidationRule(minLength);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("hi", 3)] // Under min by 1
    [InlineData("t", 2)] // Under min by 1
    [InlineData("", 1)] // Empty string under min 1
    [InlineData("hello", 10)] // With spaces under min
    [InlineData("123", 5)] // Numbers under min
    public async Task ValidateAsync_InvalidLengths_ReturnsError(string input, int minLength)
    {
        // Arrange
        var rule = new MinLengthValidationRule(minLength);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Single(result, $"Length must be at least {minLength} characters.");
    }

    [Theory]
    [InlineData(null)] // Null input
    public async Task ValidateAsync_NullInput_ReturnsEmptyErrors(string input)
    {
        // Arrange
        var rule = new MinLengthValidationRule(10);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_CustomErrorMessage_ReturnsCustomError()
    {
        // Arrange
        var customMessage = "Custom min length error";
        var rule = new MinLengthValidationRule(5, customMessage);
        var input = "hi";

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Single(result, customMessage);
    }

    [Theory]
    [InlineData(0)] // Min length 0
    [InlineData(1)] // Min length 1
    [InlineData(100)] // Min length 100
    [InlineData(1000)] // Min length 1000
    public async Task ValidateAsync_DifferentMinLengths_WorkCorrectly(int minLength)
    {
        // Arrange
        var rule = new MinLengthValidationRule(minLength);
        var inputLength = Math.Min(minLength, 1000);
        var input = new string('a', inputLength); // Create string of appropriate length

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_UnicodeCharacters_CountCorrectly()
    {
        // Arrange
        var rule = new MinLengthValidationRule(4);
        var input = "café"; // 4 characters, é is one char

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_UnicodeCharacters_UnderLimit_ReturnsError()
    {
        // Arrange
        var rule = new MinLengthValidationRule(5);
        var input = "café"; // 4 characters

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Single(result, "Length must be at least 5 characters.");
    }

    [Fact]
    public async Task ValidateAsync_EmojiCharacters_CountAsMultipleChars()
    {
        // Arrange
        var rule = new MinLengthValidationRule(1);
        var input = "👋"; // Emoji might be multiple UTF-16 code units

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        // Note: String.Length returns UTF-16 code units, not grapheme clusters
        // The behavior depends on how .NET represents the emoji
        if (input.Length >= 1)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result, "Length must be at least 1 characters.");
        }
    }

    [Fact]
    public async Task ValidateAsync_DefaultConstructor_Uses1MinLength()
    {
        // Arrange
        var rule = new MinLengthValidationRule();
        var input = ""; // Under default 1

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Single(result, "Length must be at least 1 characters.");
    }

    [Fact]
    public async Task ValidateAsync_VeryShortString_UnderMinLength_ReturnsError()
    {
        // Arrange
        var rule = new MinLengthValidationRule(10);
        var input = "hi";

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Single(result, "Length must be at least 10 characters.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyString_ValidWhenMinLengthZero()
    {
        // Arrange
        var rule = new MinLengthValidationRule(0);
        var input = "";

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var rule = new MinLengthValidationRule(10);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await rule.ValidateAsync("hello", cts.Token));
    }

    [Theory]
    [InlineData("hello\tworld", 11)] // Tab character - 11 chars total
    [InlineData("hello\nworld", 11)] // Newline character - 11 chars total
    [InlineData("hello\rworld", 11)] // Carriage return - 11 chars total
    [InlineData("hello\fworld", 11)] // Form feed - 11 chars total
    public async Task ValidateAsync_StringsWithControlCharacters_CountCorrectly(string input, int minLength)
    {
        // Act
        var result = await new MinLengthValidationRule(minLength).ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_BoundaryTest_ExactlyAtMinLength()
    {
        // Arrange
        var minLength = 10;
        var rule = new MinLengthValidationRule(minLength);
        var input = new string('a', minLength);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_BoundaryTest_OneUnderMinLength()
    {
        // Arrange
        var minLength = 10;
        var rule = new MinLengthValidationRule(minLength);
        var input = new string('a', minLength - 1);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Single(result, $"Length must be at least {minLength} characters.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task ValidateAsync_VariousMinLengths_BoundaryTests(int minLength)
    {
        // Arrange
        var rule = new MinLengthValidationRule(minLength);

        // Test exactly at boundary
        var atBoundary = new string('a', minLength);
        var resultAtBoundary = await rule.ValidateAsync(atBoundary);
        Assert.Empty(resultAtBoundary);

        // Test one under boundary
        if (minLength > 0)
        {
            var underBoundary = new string('a', minLength - 1);
            var resultUnderBoundary = await rule.ValidateAsync(underBoundary);
            Assert.Single(resultUnderBoundary, $"Length must be at least {minLength} characters.");
        }
    }

    [Fact]
    public async Task ValidateAsync_MinLengthZero_AllStringsValid()
    {
        // Arrange
        var rule = new MinLengthValidationRule(0);

        // Test empty string
        var resultEmpty = await rule.ValidateAsync("");
        Assert.Empty(resultEmpty);

        // Test any non-empty string
        var resultNonEmpty = await rule.ValidateAsync("a");
        Assert.Empty(resultNonEmpty);
    }

    [Theory]
    [InlineData("café", 4)] // Accented characters
    [InlineData("naïve", 5)] // Accented characters
    [InlineData("résumé", 6)] // Accented characters
    [InlineData("Москва", 6)] // Cyrillic
    [InlineData("東京", 2)] // Japanese
    public async Task ValidateAsync_UnicodeStrings_CountCorrectly(string input, int minLength)
    {
        // Arrange
        var rule = new MinLengthValidationRule(minLength);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        if (input.Length >= minLength)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result, $"Length must be at least {minLength} characters.");
        }
    }

    [Fact]
    public async Task ValidateAsync_ErrorMessage_IncludesCorrectMinLength()
    {
        // Arrange
        var minLength = 42;
        var rule = new MinLengthValidationRule(minLength);
        var input = "hi";

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        Assert.Single(result, $"Length must be at least {minLength} characters.");
    }
}