using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class AgeValidationRuleTests
{
    [Theory]
    [InlineData(25)]
    [InlineData(0)] // Minimum default
    [InlineData(150)] // Maximum default
    [InlineData(18)]
    [InlineData(65)]
    [InlineData(100)]
    public async Task ValidateAsync_ValidAges_ReturnsEmptyErrors(int age)
    {
        // Arrange
        var rule = new AgeValidationRule();

        // Act
        var result = await rule.ValidateAsync(age);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    public async Task ValidateAsync_NegativeAges_ReturnsError(int age)
    {
        // Arrange
        var rule = new AgeValidationRule(); // minAge=0, maxAge=150

        // Act
        var result = await rule.ValidateAsync(age);

        // Assert
        // Negative ages trigger both min age check (since age < 0) and negative check
        result.Should().HaveCount(2)
            .And.Contain("Age cannot be less than 0 years.")
            .And.Contain("Age cannot be negative.");
    }

    [Theory]
    [InlineData(151)]
    [InlineData(200)]
    [InlineData(1000)]
    public async Task ValidateAsync_AgesAboveMaximum_ReturnsError(int age)
    {
        // Arrange
        var rule = new AgeValidationRule();

        // Act
        var result = await rule.ValidateAsync(age);

        // Assert
        result.Should().ContainSingle("Age cannot exceed 150 years.");
    }

    [Fact]
    public async Task ValidateAsync_CustomMinAge_BelowMinimum_ReturnsError()
    {
        // Arrange
        var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

        // Act
        var result = await rule.ValidateAsync(16);

        // Assert
        result.Should().ContainSingle("Age cannot be less than 18 years.");
    }

    [Fact]
    public async Task ValidateAsync_CustomMaxAge_AboveMaximum_ReturnsError()
    {
        // Arrange
        var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

        // Act
        var result = await rule.ValidateAsync(70);

        // Assert
        result.Should().ContainSingle("Age cannot exceed 65 years.");
    }

    [Theory]
    [InlineData(18)]
    [InlineData(25)]
    [InlineData(65)]
    public async Task ValidateAsync_CustomRange_ValidAges_ReturnsEmptyErrors(int age)
    {
        // Arrange
        var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

        // Act
        var result = await rule.ValidateAsync(age);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_CustomRange_BoundaryValues_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

        // Act & Assert
        (await rule.ValidateAsync(18)).Should().BeEmpty(); // Min boundary
        (await rule.ValidateAsync(65)).Should().BeEmpty(); // Max boundary
    }

    [Fact]
    public async Task ValidateAsync_CustomRange_OutOfBounds_ReturnsError()
    {
        // Arrange
        var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

        // Act
        var result = await rule.ValidateAsync(200);

        // Assert
        result.Should().ContainSingle("Age cannot exceed 65 years.");
    }

    [Fact]
    public async Task ValidateAsync_NegativeAge_CustomMinAge_ReturnsMultipleErrors()
    {
        // Arrange
        var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

        // Act
        var result = await rule.ValidateAsync(-5);

        // Assert
        result.Should().HaveCount(2)
            .And.Contain("Age cannot be negative.")
            .And.Contain("Age cannot be less than 18 years.");
    }

    [Theory]
    [InlineData(0, 120)] // Default equivalent
    [InlineData(16, 100)] // Teen to century
    [InlineData(21, 25)] // Narrow range
    public async Task ValidateAsync_DifferentRanges_WorkCorrectly(int minAge, int maxAge)
    {
        // Arrange
        var rule = new AgeValidationRule(minAge, maxAge);

        // Act & Assert
        (await rule.ValidateAsync(minAge)).Should().BeEmpty();
        (await rule.ValidateAsync(maxAge)).Should().BeEmpty();

        // Test below minimum
        var belowMinResult = await rule.ValidateAsync(minAge - 1);
        if (minAge == 0)
        {
            // When minAge is 0, negative values trigger both min age check and negative check
            belowMinResult.Should().HaveCount(2)
                .And.Contain("Age cannot be less than 0 years.")
                .And.Contain("Age cannot be negative.");
        }
        else
        {
            belowMinResult.Should().ContainSingle($"Age cannot be less than {minAge} years.");
        }

        (await rule.ValidateAsync(maxAge + 1)).Should().ContainSingle($"Age cannot exceed {maxAge} years.");
    }
}