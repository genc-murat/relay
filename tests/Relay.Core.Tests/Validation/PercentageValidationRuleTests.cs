using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class PercentageValidationRuleTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(50.0)]
    [InlineData(100.0)]
    [InlineData(25.5)]
    [InlineData(99.99)]
    [InlineData(0.01)]
    public async Task ValidateAsync_ValidPercentages_ReturnsEmptyErrors(double percentage)
    {
        // Arrange
        var rule = new PercentageValidationRule();

        // Act
        var result = await rule.ValidateAsync(percentage);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task ValidateAsync_InvalidNumbers_ReturnsError(double percentage)
    {
        // Arrange
        var rule = new PercentageValidationRule();

        // Act
        var result = await rule.ValidateAsync(percentage);

        // Assert
        result.Should().ContainSingle("Percentage must be a valid number.");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1.0)]
    [InlineData(-10.0)]
    public async Task ValidateAsync_NegativePercentages_ReturnsError(double percentage)
    {
        // Arrange
        var rule = new PercentageValidationRule(); // minPercentage=0, allowNegative=false

        // Act
        var result = await rule.ValidateAsync(percentage);

        // Assert
        // Negative percentages trigger both negative check and min percentage check
        result.Should().HaveCount(2)
            .And.Contain("Percentage cannot be negative.")
            .And.Contain("Percentage cannot be less than 0%.");
    }

    [Fact]
    public async Task ValidateAsync_PercentageAboveMaximum_ReturnsError()
    {
        // Arrange
        var rule = new PercentageValidationRule();

        // Act
        var result = await rule.ValidateAsync(150.0);

        // Assert
        result.Should().ContainSingle("Percentage cannot exceed 100%.");
    }

    [Fact]
    public async Task ValidateAsync_PercentageBelowMinimum_ReturnsError()
    {
        // Arrange
        var rule = new PercentageValidationRule();

        // Act
        var result = await rule.ValidateAsync(-10.0);

        // Assert
        result.Should().HaveCount(2)
            .And.Contain("Percentage cannot be negative.")
            .And.Contain("Percentage cannot be less than 0%.");
    }

    [Fact]
    public async Task ValidateAsync_CustomRange_ValidPercentages_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new PercentageValidationRule(minPercentage: 10, maxPercentage: 90);

        // Act & Assert
        (await rule.ValidateAsync(10.0)).Should().BeEmpty();
        (await rule.ValidateAsync(50.0)).Should().BeEmpty();
        (await rule.ValidateAsync(90.0)).Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_CustomRange_OutOfBounds_ReturnsErrors()
    {
        // Arrange
        var rule = new PercentageValidationRule(minPercentage: 10, maxPercentage: 90);

        // Act & Assert
        (await rule.ValidateAsync(5.0)).Should().ContainSingle("Percentage cannot be less than 10%.");
        (await rule.ValidateAsync(95.0)).Should().ContainSingle("Percentage cannot exceed 90%.");
    }

    [Fact]
    public async Task WithNegative_AllowNegativePercentages()
    {
        // Arrange
        var rule = PercentageValidationRule.WithNegative();

        // Act & Assert
        (await rule.ValidateAsync(-50.0)).Should().BeEmpty();
        (await rule.ValidateAsync(-100.0)).Should().BeEmpty();
        (await rule.ValidateAsync(100.0)).Should().BeEmpty();
    }

    [Fact]
    public async Task WithNegative_CustomRange_WorksCorrectly()
    {
        // Arrange
        var rule = PercentageValidationRule.WithNegative(-50, 150);

        // Act & Assert
        (await rule.ValidateAsync(-50.0)).Should().BeEmpty();
        (await rule.ValidateAsync(0.0)).Should().BeEmpty();
        (await rule.ValidateAsync(150.0)).Should().BeEmpty();
        (await rule.ValidateAsync(-60.0)).Should().ContainSingle("Percentage cannot be less than -50%.");
        (await rule.ValidateAsync(160.0)).Should().ContainSingle("Percentage cannot exceed 150%.");
    }

    [Fact]
    public async Task Standard_CreatesStandardRule()
    {
        // Arrange
        var rule = PercentageValidationRule.Standard();

        // Act & Assert
        (await rule.ValidateAsync(0.0)).Should().BeEmpty();
        (await rule.ValidateAsync(100.0)).Should().BeEmpty();
        // Negative values trigger both negative check and min percentage check
        (await rule.ValidateAsync(-1.0)).Should().HaveCount(2)
            .And.Contain("Percentage cannot be negative.")
            .And.Contain("Percentage cannot be less than 0%.");
        (await rule.ValidateAsync(101.0)).Should().ContainSingle("Percentage cannot exceed 100%.");
    }

    [Theory]
    [InlineData(1.23456, 5)] // 5 decimal places
    [InlineData(1.123456, 6)] // 6 decimal places
    [InlineData(1.000001, 6)] // 6 decimal places
    public async Task ValidateAsync_TooManyDecimalPlaces_ReturnsError(double percentage, int decimalPlaces)
    {
        // Arrange
        var rule = new PercentageValidationRule();

        // Act
        var result = await rule.ValidateAsync(percentage);

        // Assert
        result.Should().ContainSingle("Percentage has too many decimal places (maximum 4 allowed).");
    }

    [Theory]
    [InlineData(1.0, 1)] // 1 decimal place
    [InlineData(1.12, 2)] // 2 decimal places
    [InlineData(1.123, 3)] // 3 decimal places
    [InlineData(1.1234, 4)] // 4 decimal places
    [InlineData(1.12345, 5)] // 5 decimal places - should fail
    public async Task ValidateAsync_DecimalPlacesBoundary_Tests(double percentage, int expectedDecimalPlaces)
    {
        // Arrange
        var rule = new PercentageValidationRule();

        // Act
        var result = await rule.ValidateAsync(percentage);

        // Assert
        if (expectedDecimalPlaces <= 4)
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Percentage has too many decimal places (maximum 4 allowed).");
        }
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(1.0, 1)]
    [InlineData(10.0, 1)]
    [InlineData(100.0, 1)]
    public async Task ValidateAsync_WholeNumbers_Valid(double percentage, int decimalPlaces)
    {
        // Arrange
        var rule = new PercentageValidationRule();

        // Act
        var result = await rule.ValidateAsync(percentage);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var rule = new PercentageValidationRule(minPercentage: 10, maxPercentage: 90);

        // Act
        var result = await rule.ValidateAsync(1.123456); // Below min, too many decimals

        // Assert
        result.Should().HaveCount(2)
            .And.Contain("Percentage cannot be less than 10%.")
            .And.Contain("Percentage has too many decimal places (maximum 4 allowed).");
    }
}