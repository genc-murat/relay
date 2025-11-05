using System.Linq;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

namespace Relay.Core.Tests.Validation;

public class CurrencyAmountValidationRuleTests
{
    [Theory]
    [InlineData("0.00")]
    [InlineData("100.50")]
    [InlineData("999999999999.99")]
    [InlineData("1.0")]
    [InlineData("0.01")]
    [InlineData("123456789012.34")]
    public async Task ValidateAsync_ValidCurrencyAmounts_ReturnsEmptyErrors(string amount)
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule();

        // Act
        var result = await rule.ValidateAsync(amount);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("   ")] // Whitespace
    public async Task ValidateAsync_EmptyOrWhitespace_ReturnsEmptyErrors(string amount)
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule();

        // Act
        var result = await rule.ValidateAsync(amount);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("abc")] // Non-numeric
    [InlineData("12.34.56")] // Multiple decimals
    [InlineData("12,34")] // Comma separator
    [InlineData("$12.34")] // Currency symbol
    [InlineData("12.34 USD")] // With currency code
    [InlineData("1,234.56")] // Thousands separator
    public async Task ValidateAsync_InvalidFormats_ReturnsError(string amount)
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule();

        // Act
        var result = await rule.ValidateAsync(amount);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid currency amount format.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_NegativeAmount_WhenNotAllowed_ReturnsError()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(allowNegative: false);

        // Act
        var result = await rule.ValidateAsync("-100.00");

        // Assert
        Assert.Single(result);
        Assert.Equal("Currency amount cannot be negative.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_NegativeAmount_WhenAllowed_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(allowNegative: true);

        // Act
        var result = await rule.ValidateAsync("-100.00");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_AmountBelowMinimum_ReturnsError()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(minAmount: 10.00M);

        // Act
        var result = await rule.ValidateAsync("5.00");

        // Assert
        Assert.Single(result);
        Assert.Contains("Currency amount cannot be less than", result.First());
    }

    [Fact]
    public async Task ValidateAsync_AmountAboveMaximum_ReturnsError()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(maxAmount: 1000.00M);

        // Act
        var result = await rule.ValidateAsync("1500.00");

        // Assert
        Assert.Single(result);
        Assert.Contains("Currency amount cannot exceed", result.First());
    }

    [Theory]
    [InlineData("1.234", 3)] // 3 decimal places
    [InlineData("1.2345", 4)] // 4 decimal places
    [InlineData("1.23456", 5)] // 5 decimal places - should fail for default (2)
    public async Task ValidateAsync_DecimalPlaces_DefaultTwoMax_ReturnsAppropriateResults(string amount, int decimalPlaces)
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule();

        // Act
        var result = await rule.ValidateAsync(amount);

        // Assert
        if (decimalPlaces <= 2)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Currency amount cannot have more than 2 decimal places.", result.First());
        }
    }

    [Fact]
    public async Task ValidateAsync_CustomDecimalPlaces_ReturnsAppropriateResults()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(maxDecimalPlaces: 4);

        // Act & Assert
        Assert.Empty(await rule.ValidateAsync("1.2345")); // 4 decimal places - valid
        var result = await rule.ValidateAsync("1.23456");
        Assert.Single(result);
        Assert.Equal("Currency amount cannot have more than 4 decimal places.", result.First()); // 5 decimal places - invalid
    }

    [Fact]
    public async Task Standard_CreatesStandardRule()
    {
        // Arrange
        var rule = CurrencyAmountValidationRule.Standard();

        // Act & Assert
        Assert.Empty(await rule.ValidateAsync("100.00"));
        var result1 = await rule.ValidateAsync("-100.00");
        Assert.Single(result1);
        Assert.Equal("Currency amount cannot be negative.", result1.First());
        var result2 = await rule.ValidateAsync("100.001");
        Assert.Single(result2);
        Assert.Equal("Currency amount cannot have more than 2 decimal places.", result2.First());
    }

    [Fact]
    public async Task Crypto_CreatesCryptoRule()
    {
        // Arrange
        var rule = CurrencyAmountValidationRule.Crypto();

        // Act & Assert
        Assert.Empty(await rule.ValidateAsync("0.00000001")); // 8 decimal places - valid for crypto
        var result1 = await rule.ValidateAsync("-0.00000001");
        Assert.Single(result1);
        Assert.Equal("Currency amount cannot be negative.", result1.First());
        var result2 = await rule.ValidateAsync("0.000000001");
        Assert.Single(result2);
        Assert.Equal("Invalid currency amount format.", result2.First());
    }

    [Theory]
    [InlineData("01.00")] // Leading zero
    [InlineData("001.00")] // Multiple leading zeros
    [InlineData("0100.00")] // Leading zero in larger number
    public async Task ValidateAsync_LeadingZeros_ReturnsError(string amount)
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule();

        // Act
        var result = await rule.ValidateAsync(amount);

        // Assert
        Assert.Single(result);
        Assert.Equal("Currency amount cannot have leading zeros.", result.First());
    }

    [Theory]
    [InlineData("0.00")] // Single zero
    [InlineData("0.12")] // Zero with decimals
    [InlineData("100.00")] // Valid number
    public async Task ValidateAsync_NoLeadingZeros_ReturnsEmptyErrors(string amount)
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule();

        // Act
        var result = await rule.ValidateAsync(amount);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(maxDecimalPlaces: 2, minAmount: 10, maxAmount: 100, allowNegative: false);

        // Act
        var result = await rule.ValidateAsync("-150.123");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains("Currency amount cannot have more than 2 decimal places.", result);
        Assert.Contains("Currency amount cannot be negative.", result);
    }

    [Theory]
    [InlineData("999999999999.99")] // Maximum default
    [InlineData("0.00")] // Minimum default
    public async Task ValidateAsync_BoundaryValues_ReturnsEmptyErrors(string amount)
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule();

        // Act
        var result = await rule.ValidateAsync(amount);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_CustomRange_BoundaryValues_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(minAmount: 10.00M, maxAmount: 100.00M);

        // Act & Assert
        Assert.Empty(await rule.ValidateAsync("10.00"));
        Assert.Empty(await rule.ValidateAsync("100.00"));
    }
}