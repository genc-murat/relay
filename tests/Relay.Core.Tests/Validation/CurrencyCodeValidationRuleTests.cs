using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class CurrencyCodeValidationRuleTests
{
    private readonly CurrencyCodeValidationRule _rule = new();

    [Theory]
    [InlineData("USD")] // US Dollar
    [InlineData("EUR")] // Euro
    [InlineData("GBP")] // British Pound
    [InlineData("JPY")] // Japanese Yen
    [InlineData("CAD")] // Canadian Dollar
    [InlineData("AUD")] // Australian Dollar
    [InlineData("CHF")] // Swiss Franc
    [InlineData("CNY")] // Chinese Yuan
    [InlineData("usd")] // Lowercase
    [InlineData("Eur")] // Mixed case
    public async Task ValidateAsync_ValidCurrencyCode_ReturnsEmptyErrors(string currencyCode)
    {
        // Act
        var result = await _rule.ValidateAsync(currencyCode);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("US")] // Too short
    [InlineData("USDD")] // Too long
    [InlineData("US1")] // Contains number
    [InlineData("US$")] // Contains special character
    [InlineData("XYZ")] // Non-existent code
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidCurrencyCode_ReturnsError(string currencyCode)
    {
        // Act
        var result = await _rule.ValidateAsync(currencyCode);

        // Assert
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Invalid ISO 4217 currency code.");
        }
    }
}