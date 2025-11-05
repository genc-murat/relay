 using System;
 using System.Linq;
 using System.Threading.Tasks;
 using Xunit;
 using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class DateTimeValidationRulesTests
    {
        [Fact]
        public async Task TodayValidationRule_Should_Validate_Today()
        {
            // Arrange
            var rule = new TodayValidationRule();
            var today = DateTime.Today;
            var yesterday = DateTime.Today.AddDays(-1);

            // Act & Assert
            var validResult = await rule.ValidateAsync(today);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(yesterday);
            Assert.Single(invalidResult);
            Assert.Contains("must be today", invalidResult.First().ToLower());
        }

        [Theory]
        [InlineData(25)]
        [InlineData(0)] // Minimum default
        [InlineData(150)] // Maximum default
        [InlineData(18)]
        [InlineData(65)]
        [InlineData(100)]
        public async Task AgeValidationRule_ValidateAsync_ValidAges_ReturnsEmptyErrors(int age)
        {
            // Arrange
            var rule = new AgeValidationRule();

            // Act
            var result = await rule.ValidateAsync(age);

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(-100)]
        public async Task AgeValidationRule_ValidateAsync_NegativeAges_ReturnsError(int age)
        {
            // Arrange
            var rule = new AgeValidationRule(); // minAge=0, maxAge=150

            // Act
            var result = await rule.ValidateAsync(age);

            // Assert
            // Negative ages trigger both min age check (since age < 0) and negative check
            Assert.Equal(2, result.Count());
            Assert.Contains("Age cannot be less than 0 years.", result);
            Assert.Contains("Age cannot be negative.", result);
        }

        [Theory]
        [InlineData(151)]
        [InlineData(200)]
        [InlineData(1000)]
        public async Task AgeValidationRule_ValidateAsync_AgesAboveMaximum_ReturnsError(int age)
        {
            // Arrange
            var rule = new AgeValidationRule();

            // Act
            var result = await rule.ValidateAsync(age);

            // Assert
            Assert.Single(result);
            Assert.Equal("Age cannot exceed 150 years.", result.First());
        }

        [Fact]
        public async Task AgeValidationRule_ValidateAsync_CustomMinAge_BelowMinimum_ReturnsError()
        {
            // Arrange
            var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

            // Act
            var result = await rule.ValidateAsync(16);

            // Assert
            Assert.Single(result);
            Assert.Equal("Age cannot be less than 18 years.", result.First());
        }

        [Fact]
        public async Task AgeValidationRule_ValidateAsync_CustomMaxAge_AboveMaximum_ReturnsError()
        {
            // Arrange
            var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

            // Act
            var result = await rule.ValidateAsync(70);

            // Assert
            Assert.Single(result);
            Assert.Equal("Age cannot exceed 65 years.", result.First());
        }

        [Theory]
        [InlineData(18)]
        [InlineData(25)]
        [InlineData(65)]
        public async Task AgeValidationRule_ValidateAsync_CustomRange_ValidAges_ReturnsEmptyErrors(int age)
        {
            // Arrange
            var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

            // Act
            var result = await rule.ValidateAsync(age);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task AgeValidationRule_ValidateAsync_CustomRange_BoundaryValues_ReturnsEmptyErrors()
        {
            // Arrange
            var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(18)); // Min boundary
            Assert.Empty(await rule.ValidateAsync(65)); // Max boundary
        }

        [Fact]
        public async Task AgeValidationRule_ValidateAsync_CustomRange_OutOfBounds_ReturnsError()
        {
            // Arrange
            var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

            // Act
            var result = await rule.ValidateAsync(200);

            // Assert
            Assert.Single(result);
            Assert.Equal("Age cannot exceed 65 years.", result.First());
        }

        [Fact]
        public async Task AgeValidationRule_ValidateAsync_NegativeAge_CustomMinAge_ReturnsMultipleErrors()
        {
            // Arrange
            var rule = new AgeValidationRule(minAge: 18, maxAge: 65);

            // Act
            var result = await rule.ValidateAsync(-5);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains("Age cannot be negative.", result);
            Assert.Contains("Age cannot be less than 18 years.", result);
        }

        [Theory]
        [InlineData(0, 120)] // Default equivalent
        [InlineData(16, 100)] // Teen to century
        [InlineData(21, 25)] // Narrow range
        public async Task AgeValidationRule_ValidateAsync_DifferentRanges_WorkCorrectly(int minAge, int maxAge)
        {
            // Arrange
            var rule = new AgeValidationRule(minAge, maxAge);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(minAge));
            Assert.Empty(await rule.ValidateAsync(maxAge));

            // Test below minimum
            var belowMinResult = await rule.ValidateAsync(minAge - 1);
            if (minAge == 0)
            {
                // When minAge is 0, negative values trigger both min age check and negative check
                Assert.Equal(2, belowMinResult.Count());
                Assert.Contains("Age cannot be less than 0 years.", belowMinResult);
                Assert.Contains("Age cannot be negative.", belowMinResult);
            }
            else
            {
                Assert.Single(belowMinResult);
                Assert.Equal($"Age cannot be less than {minAge} years.", belowMinResult.First());
            }

            var aboveMaxResult = await rule.ValidateAsync(maxAge + 1);
            Assert.Single(aboveMaxResult);
            Assert.Equal($"Age cannot exceed {maxAge} years.", aboveMaxResult.First());
        }
    }
}