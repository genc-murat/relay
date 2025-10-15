using System.Threading.Tasks;
using FluentAssertions;
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
        result.Should().BeEmpty();
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
        result.Should().BeEmpty();
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
        result.Should().ContainSingle("Invalid currency amount format.");
    }

    [Fact]
    public async Task ValidateAsync_NegativeAmount_WhenNotAllowed_ReturnsError()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(allowNegative: false);

        // Act
        var result = await rule.ValidateAsync("-100.00");

        // Assert
        result.Should().ContainSingle("Currency amount cannot be negative.");
    }

    [Fact]
    public async Task ValidateAsync_NegativeAmount_WhenAllowed_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(allowNegative: true);

        // Act
        var result = await rule.ValidateAsync("-100.00");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_AmountBelowMinimum_ReturnsError()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(minAmount: 10.00M);

        // Act
        var result = await rule.ValidateAsync("5.00");

        // Assert
        result.Should().ContainSingle().Which.Should().Contain("Currency amount cannot be less than");
    }

    [Fact]
    public async Task ValidateAsync_AmountAboveMaximum_ReturnsError()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(maxAmount: 1000.00M);

        // Act
        var result = await rule.ValidateAsync("1500.00");

        // Assert
        result.Should().ContainSingle().Which.Should().Contain("Currency amount cannot exceed");
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
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Currency amount cannot have more than 2 decimal places.");
        }
    }

    [Fact]
    public async Task ValidateAsync_CustomDecimalPlaces_ReturnsAppropriateResults()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(maxDecimalPlaces: 4);

        // Act & Assert
        (await rule.ValidateAsync("1.2345")).Should().BeEmpty(); // 4 decimal places - valid
        (await rule.ValidateAsync("1.23456")).Should().ContainSingle("Currency amount cannot have more than 4 decimal places."); // 5 decimal places - invalid
    }

    [Fact]
    public async Task Standard_CreatesStandardRule()
    {
        // Arrange
        var rule = CurrencyAmountValidationRule.Standard();

        // Act & Assert
        (await rule.ValidateAsync("100.00")).Should().BeEmpty();
        (await rule.ValidateAsync("-100.00")).Should().ContainSingle("Currency amount cannot be negative.");
        (await rule.ValidateAsync("100.001")).Should().ContainSingle("Currency amount cannot have more than 2 decimal places.");
    }

    [Fact]
    public async Task Crypto_CreatesCryptoRule()
    {
        // Arrange
        var rule = CurrencyAmountValidationRule.Crypto();

        // Act & Assert
        (await rule.ValidateAsync("0.00000001")).Should().BeEmpty(); // 8 decimal places - valid for crypto
        (await rule.ValidateAsync("-0.00000001")).Should().ContainSingle("Currency amount cannot be negative.");
        (await rule.ValidateAsync("0.000000001")).Should().ContainSingle("Currency amount cannot have more than 8 decimal places.");
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
        result.Should().ContainSingle("Currency amount cannot have leading zeros.");
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
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(maxDecimalPlaces: 2, minAmount: 10, maxAmount: 100, allowNegative: false);

        // Act
        var result = await rule.ValidateAsync("-150.123");

        // Assert
        result.Should().HaveCount(2)
            .And.Contain("Currency amount cannot have more than 2 decimal places.")
            .And.Contain("Currency amount cannot be negative.");
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
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_CustomRange_BoundaryValues_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new CurrencyAmountValidationRule(minAmount: 10.00M, maxAmount: 100.00M);

        // Act & Assert
        (await rule.ValidateAsync("10.00")).Should().BeEmpty();
        (await rule.ValidateAsync("100.00")).Should().BeEmpty();
    }
}