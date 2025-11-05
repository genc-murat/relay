using Relay.Core.Validation.Rules;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class IsLowerCaseValidationRuleTests
{
    private readonly IsLowerCaseValidationRule _rule = new();

    [Theory]
    [InlineData("hello")]
    [InlineData("world")]
    [InlineData("lowercase")]
    [InlineData("abcdefghijklmnopqrstuvwxyz")]
    [InlineData("a")]
    [InlineData("test")]
    public async Task ValidateAsync_ValidLowerCaseStrings_ReturnsEmptyErrors(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("Hello")] // Capital H
    [InlineData("WORLD")] // All uppercase
    [InlineData("Lowercase")] // Capital L
    [InlineData("ABC")] // All uppercase
    [InlineData("Test")] // Capital T
    [InlineData("HELLO WORLD")] // All uppercase with space
    [InlineData("Hello World")] // Mixed case with space
    public async Task ValidateAsync_InvalidUpperCaseStrings_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("hello123")] // Numbers
    [InlineData("hello!")] // Special characters
    [InlineData("hello@world")] // Special characters
    [InlineData("hello-world")] // Hyphen
    [InlineData("hello_world")] // Underscore
    [InlineData("hello.world")] // Dot
    [InlineData("multiple words")] // Spaces
    [InlineData("sentence with spaces")] // Spaces
    public async Task ValidateAsync_StringsWithNonLetters_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("")] // Empty string
    [InlineData(null)] // Null
    public async Task ValidateAsync_EmptyOrNullStrings_ReturnsEmptyErrors(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("   ")] // Spaces only
    [InlineData("\t")] // Tab
    [InlineData("\n")] // Newline
    [InlineData("\r")] // Carriage return
    [InlineData(" \t\n\r ")] // Mixed whitespace
    public async Task ValidateAsync_WhitespaceOnlyStrings_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("café")] // Accented lowercase
    [InlineData("naïve")] // Accented lowercase
    [InlineData("résumé")] // Accented lowercase
    [InlineData("piñata")] // Accented lowercase
    public async Task ValidateAsync_AccentedLowerCaseLetters_ReturnsEmptyErrors(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("Café")] // Accented with uppercase
    [InlineData("NAÏVE")] // Accented uppercase
    [InlineData("Résumé")] // Accented mixed case
    public async Task ValidateAsync_AccentedUpperCaseLetters_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("привет")] // Cyrillic lowercase
    [InlineData("мир")] // Cyrillic lowercase
    [InlineData("тест")] // Cyrillic lowercase
    public async Task ValidateAsync_CyrillicLowerCaseLetters_ReturnsEmptyErrors(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("ПРИВЕТ")] // Cyrillic uppercase
    [InlineData("МИР")] // Cyrillic uppercase
    [InlineData("Привет")] // Cyrillic mixed case
    public async Task ValidateAsync_CyrillicUpperCaseLetters_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("مرحبا")] // Arabic
    [InlineData("こんにちは")] // Japanese Hiragana
    [InlineData("你好")] // Chinese
    [InlineData("안녕하세요")] // Korean
    public async Task ValidateAsync_NonLatinScripts_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("مرحبا")] // Arabic (no uppercase equivalent)
    [InlineData("こんにちは")] // Japanese Hiragana (no uppercase)
    [InlineData("你好")] // Chinese (no uppercase)
    [InlineData("안녕하세요")] // Korean (no uppercase)
    public async Task ValidateAsync_NonLatinScriptsWithoutCase_ReturnsEmptyErrors(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("hello\x00world")] // Null character
    [InlineData("hello\u0001world")] // Control character
    [InlineData("hello\u007Fworld")] // Delete character
    public async Task ValidateAsync_StringsWithControlCharacters_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("a\u0300")] // Combining character
    [InlineData("e\u0301")] // Combining acute
    [InlineData("n\u0303")] // Combining tilde
    public async Task ValidateAsync_StringsWithCombiningCharacters_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _rule.ValidateAsync("hello", cts.Token));
    }

    [Theory]
    [InlineData("a")] // Single lowercase letter
    [InlineData("z")] // Last lowercase letter
    [InlineData("m")] // Middle lowercase letter
    public async Task ValidateAsync_SingleLowerCaseLetters_ReturnsEmptyErrors(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("A")] // Single uppercase letter
    [InlineData("Z")] // Last uppercase letter
    [InlineData("M")] // Middle uppercase letter
    public async Task ValidateAsync_SingleUpperCaseLetters_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("hello\nworld")] // Newline
    [InlineData("hello\tworld")] // Tab
    [InlineData("hello\rworld")] // Carriage return
    [InlineData("hello\fworld")] // Form feed
    [InlineData("hello\vworld")] // Vertical tab
    public async Task ValidateAsync_StringsWithWhitespaceCharacters_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }

    [Theory]
    [InlineData("123")] // Only numbers
    [InlineData("!@#$")] // Only special chars
    [InlineData("123abc!@#")] // Mixed invalid chars
    public async Task ValidateAsync_StringsWithOnlyNonLetters_ReturnsError(string input)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        Assert.Single(result); Assert.Equal("Value must consist only of lowercase letters.", result.First());
    }
}