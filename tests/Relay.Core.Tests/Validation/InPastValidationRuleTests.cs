using System;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class InPastValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Past_DateTime()
        {
            // Arrange
            var rule = new InPastValidationRule<DateTime>(() => DateTime.Now);
            var pastDateTime = DateTime.Now.AddDays(-1);

            // Act
            var errors = await rule.ValidateAsync(pastDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Future_DateTime()
        {
            // Arrange
            var rule = new InPastValidationRule<DateTime>(() => DateTime.Now);
            var futureDateTime = DateTime.Now.AddDays(1);

            // Act
            var errors = await rule.ValidateAsync(futureDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be in the past.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Past_DateTimeOffset()
        {
            // Arrange
            var rule = new InPastValidationRule<DateTimeOffset>(() => DateTimeOffset.Now);
            var pastDateTime = DateTimeOffset.Now.AddDays(-1);

            // Act
            var errors = await rule.ValidateAsync(pastDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Future_DateTimeOffset()
        {
            // Arrange
            var rule = new InPastValidationRule<DateTimeOffset>(() => DateTimeOffset.Now);
            var futureDateTime = DateTimeOffset.Now.AddDays(1);

            // Act
            var errors = await rule.ValidateAsync(futureDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be in the past.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Smaller_Int()
        {
            // Arrange
            var rule = new InPastValidationRule<int>(() => 10);
            var value = 9;

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Greater_Int()
        {
            // Arrange
            var rule = new InPastValidationRule<int>(() => 10);
            var value = 11;

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be in the past.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message()
        {
            // Arrange
            var rule = new InPastValidationRule<DateTime>(() => DateTime.Now, "Date must be earlier.");
            var futureDateTime = DateTime.Now.AddDays(1);

            // Act
            var errors = await rule.ValidateAsync(futureDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Date must be earlier.", errors.First());
        }
    }
}