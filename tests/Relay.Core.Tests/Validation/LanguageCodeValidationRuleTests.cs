using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class LanguageCodeValidationRuleTests
{
    private readonly LanguageCodeValidationRule _rule = new();

    [Theory]
    [InlineData("en")] // English
    [InlineData("es")] // Spanish
    [InlineData("fr")] // French
    [InlineData("de")] // German
    [InlineData("zh")] // Chinese
    [InlineData("ja")] // Japanese
    [InlineData("EN")] // Uppercase
    [InlineData("En")] // Mixed case
    [InlineData("eng")] // 3-letter code
    [InlineData("spa")] // 3-letter code
    public async Task ValidateAsync_ValidLanguageCode_ReturnsEmptyErrors(string languageCode)
    {
        // Act
        var result = await _rule.ValidateAsync(languageCode);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("e")] // Too short
    [InlineData("engl")] // Too long
    [InlineData("e1")] // Contains number
    [InlineData("e$")] // Contains special character
    [InlineData("xyz")] // Non-existent code
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidLanguageCode_ReturnsError(string languageCode)
    {
        // Act
        var result = await _rule.ValidateAsync(languageCode);

        // Assert
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Invalid ISO 639 language code.");
        }
    }
}