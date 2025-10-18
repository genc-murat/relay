using System;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class TimeSpanValidationRuleTests
    {        
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_TimeSpan_Is_In_Range()
        {
            // Arrange
            var minValue = TimeSpan.FromSeconds(10);
            var maxValue = TimeSpan.FromSeconds(20);
            var rule = new TimeSpanValidationRule(minValue, maxValue);
            var validTimeSpan = TimeSpan.FromSeconds(15);

            // Act
            var errors = await rule.ValidateAsync(validTimeSpan);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_TimeSpan_Is_Less_Than_Min()
        {
            // Arrange
            var minValue = TimeSpan.FromSeconds(10);
            var rule = new TimeSpanValidationRule(minValue: minValue);
            var invalidTimeSpan = TimeSpan.FromSeconds(5);

            // Act
            var errors = await rule.ValidateAsync(invalidTimeSpan);

            // Assert
            Assert.Single(errors);
            Assert.Equal($"Timespan must be greater than or equal to {minValue}.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_TimeSpan_Is_Greater_Than_Max()
        {            
            // Arrange
            var maxValue = TimeSpan.FromSeconds(20);
            var rule = new TimeSpanValidationRule(maxValue: maxValue);
            var invalidTimeSpan = TimeSpan.FromSeconds(25);

            // Act
            var errors = await rule.ValidateAsync(invalidTimeSpan);

            // Assert
            Assert.Single(errors);
            Assert.Equal($"Timespan must be less than or equal to {maxValue}.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message()
        {
            // Arrange
            var rule = new TimeSpanValidationRule(minValue: TimeSpan.FromHours(1), errorMessage: "Duration is too short.");
            var invalidTimeSpan = TimeSpan.FromMinutes(30);

            // Act
            var errors = await rule.ValidateAsync(invalidTimeSpan);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Duration is too short.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_No_Bounds_Specified()
        {
            // Arrange
            var rule = new TimeSpanValidationRule();
            var anyTimeSpan = TimeSpan.FromDays(1);

            // Act
            var errors = await rule.ValidateAsync(anyTimeSpan);

            // Assert
            Assert.Empty(errors);
        }
    }
}