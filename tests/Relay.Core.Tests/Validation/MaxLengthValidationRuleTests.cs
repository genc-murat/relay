using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class MaxLengthValidationRuleTests
{
    [Theory]
    [InlineData("hello", 10)] // Well below max
    [InlineData("test", 4)] // Exactly at max
    [InlineData("", 0)] // Empty string at max 0
    [InlineData("a", 5)] // Single char below max
    [InlineData("hello world", 20)] // With spaces
    [InlineData("1234567890", 10)] // Numbers
    [InlineData("!@#$%^&*()", 10)] // Special chars
    public async Task ValidateAsync_ValidLengths_ReturnsEmptyErrors(string input, int maxLength)
    {
        // Arrange
        var rule = new MaxLengthValidationRule(maxLength);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("hello", 3)] // Over max by 2
    [InlineData("test", 2)] // Over max by 2
    [InlineData("a", 0)] // Single char over max 0
    [InlineData("hello world", 5)] // With spaces over max
    [InlineData("1234567890", 5)] // Numbers over max
    public async Task ValidateAsync_InvalidLengths_ReturnsError(string input, int maxLength)
    {
        // Arrange
        var rule = new MaxLengthValidationRule(maxLength);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().ContainSingle($"Length must not exceed {maxLength} characters.");
    }

    [Theory]
    [InlineData(null)] // Null input
    public async Task ValidateAsync_NullInput_ReturnsEmptyErrors(string input)
    {
        // Arrange
        var rule = new MaxLengthValidationRule(10);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_CustomErrorMessage_ReturnsCustomError()
    {
        // Arrange
        var customMessage = "Custom max length error";
        var rule = new MaxLengthValidationRule(3, customMessage);
        var input = "hello";

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().ContainSingle(customMessage);
    }

    [Theory]
    [InlineData(0)] // Max length 0
    [InlineData(1)] // Max length 1
    [InlineData(100)] // Max length 100
    [InlineData(1000)] // Max length 1000
    [InlineData(int.MaxValue)] // Very large max length
    public async Task ValidateAsync_DifferentMaxLengths_WorkCorrectly(int maxLength)
    {
        // Arrange
        var rule = new MaxLengthValidationRule(maxLength);
        var input = new string('a', Math.Min(maxLength, 1000)); // Create string of appropriate length

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_UnicodeCharacters_CountCorrectly()
    {
        // Arrange
        var rule = new MaxLengthValidationRule(5);
        var input = "café"; // 4 characters, é is one char

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_UnicodeCharacters_OverLimit_ReturnsError()
    {
        // Arrange
        var rule = new MaxLengthValidationRule(3);
        var input = "café"; // 4 characters

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().ContainSingle("Length must not exceed 3 characters.");
    }

    [Fact]
    public async Task ValidateAsync_EmojiCharacters_CountAsMultipleChars()
    {
        // Arrange
        var rule = new MaxLengthValidationRule(2);
        var input = "👋"; // Emoji might be multiple UTF-16 code units

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        // Note: String.Length returns UTF-16 code units, not grapheme clusters
        // The behavior depends on how .NET represents the emoji
        if (input.Length <= 2)
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Length must not exceed 2 characters.");
        }
    }

    [Fact]
    public async Task ValidateAsync_DefaultConstructor_Uses255MaxLength()
    {
        // Arrange
        var rule = new MaxLengthValidationRule();
        var input = new string('a', 256); // Over default 255

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().ContainSingle("Length must not exceed 255 characters.");
    }

    [Fact]
    public async Task ValidateAsync_VeryLongString_OverMaxLength_ReturnsError()
    {
        // Arrange
        var rule = new MaxLengthValidationRule(100);
        var input = new string('a', 200);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().ContainSingle("Length must not exceed 100 characters.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyString_AlwaysValid()
    {
        // Arrange
        var rule = new MaxLengthValidationRule(0);
        var input = "";

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var rule = new MaxLengthValidationRule(10);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await rule.ValidateAsync("hello", cts.Token));
    }

    [Theory]
    [InlineData("hello\tworld", 12)] // Tab character - 11 chars total
    [InlineData("hello\nworld", 12)] // Newline character - 11 chars total
    [InlineData("hello\rworld", 12)] // Carriage return - 11 chars total
    [InlineData("hello\fworld", 12)] // Form feed - 11 chars total
    public async Task ValidateAsync_StringsWithControlCharacters_CountCorrectly(string input, int maxLength)
    {
        // Act
        var result = await new MaxLengthValidationRule(maxLength).ValidateAsync(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_BoundaryTest_ExactlyAtMaxLength()
    {
        // Arrange
        var maxLength = 10;
        var rule = new MaxLengthValidationRule(maxLength);
        var input = new string('a', maxLength);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_BoundaryTest_OneOverMaxLength()
    {
        // Arrange
        var maxLength = 10;
        var rule = new MaxLengthValidationRule(maxLength);
        var input = new string('a', maxLength + 1);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().ContainSingle($"Length must not exceed {maxLength} characters.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task ValidateAsync_VariousMaxLengths_BoundaryTests(int maxLength)
    {
        // Arrange
        var rule = new MaxLengthValidationRule(maxLength);

        // Test exactly at boundary
        var atBoundary = new string('a', maxLength);
        var resultAtBoundary = await rule.ValidateAsync(atBoundary);
        resultAtBoundary.Should().BeEmpty();

        // Test one over boundary
        var overBoundary = new string('a', maxLength + 1);
        var resultOverBoundary = await rule.ValidateAsync(overBoundary);
        resultOverBoundary.Should().ContainSingle($"Length must not exceed {maxLength} characters.");
    }

    [Fact]
    public async Task ValidateAsync_MaxLengthZero_OnlyEmptyStringValid()
    {
        // Arrange
        var rule = new MaxLengthValidationRule(0);

        // Test empty string
        var resultEmpty = await rule.ValidateAsync("");
        resultEmpty.Should().BeEmpty();

        // Test any non-empty string
        var resultNonEmpty = await rule.ValidateAsync("a");
        resultNonEmpty.Should().ContainSingle("Length must not exceed 0 characters.");
    }

    [Theory]
    [InlineData("café", 4)] // Accented characters
    [InlineData("naïve", 5)] // Accented characters
    [InlineData("résumé", 6)] // Accented characters
    [InlineData("Москва", 6)] // Cyrillic
    [InlineData("東京", 2)] // Japanese
    public async Task ValidateAsync_UnicodeStrings_CountCorrectly(string input, int maxLength)
    {
        // Arrange
        var rule = new MaxLengthValidationRule(maxLength);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        if (input.Length <= maxLength)
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle($"Length must not exceed {maxLength} characters.");
        }
    }

    [Fact]
    public async Task ValidateAsync_ErrorMessage_IncludesCorrectMaxLength()
    {
        // Arrange
        var maxLength = 42;
        var rule = new MaxLengthValidationRule(maxLength);
        var input = new string('a', maxLength + 1);

        // Act
        var result = await rule.ValidateAsync(input);

        // Assert
        result.Should().ContainSingle($"Length must not exceed {maxLength} characters.");
    }
}