using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class CountryCodeValidationRuleTests
{
    private readonly CountryCodeValidationRule _rule = new();

    [Theory]
    [InlineData("US")] // United States
    [InlineData("GB")] // United Kingdom
    [InlineData("DE")] // Germany
    [InlineData("FR")] // France
    [InlineData("JP")] // Japan
    [InlineData("CN")] // China
    [InlineData("us")] // Lowercase
    [InlineData("Us")] // Mixed case
    [InlineData("USA")] // 3-letter code
    [InlineData("GBR")] // 3-letter code
    public async Task ValidateAsync_ValidCountryCode_ReturnsEmptyErrors(string countryCode)
    {
        // Act
        var result = await _rule.ValidateAsync(countryCode);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("U")] // Too short
    [InlineData("USAA")] // Too long
    [InlineData("U1")] // Contains number
    [InlineData("U$")] // Contains special character
    [InlineData("XYZ")] // Non-existent code
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidCountryCode_ReturnsError(string countryCode)
    {
        // Act
        var result = await _rule.ValidateAsync(countryCode);

        // Assert
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Invalid ISO 3166 country code.", result.Single());
        }
    }
}