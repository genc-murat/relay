using System;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class InFutureValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Future_DateTime()
        {
            // Arrange
            var rule = new InFutureValidationRule<DateTime>(() => DateTime.Now);
            var futureDateTime = DateTime.Now.AddDays(1);

            // Act
            var errors = await rule.ValidateAsync(futureDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Past_DateTime()
        {
            // Arrange
            var rule = new InFutureValidationRule<DateTime>(() => DateTime.Now);
            var pastDateTime = DateTime.Now.AddDays(-1);

            // Act
            var errors = await rule.ValidateAsync(pastDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be in the future.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Future_DateTimeOffset()
        {
            // Arrange
            var rule = new InFutureValidationRule<DateTimeOffset>(() => DateTimeOffset.Now);
            var futureDateTime = DateTimeOffset.Now.AddDays(1);

            // Act
            var errors = await rule.ValidateAsync(futureDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Past_DateTimeOffset()
        {
            // Arrange
            var rule = new InFutureValidationRule<DateTimeOffset>(() => DateTimeOffset.Now);
            var pastDateTime = DateTimeOffset.Now.AddDays(-1);

            // Act
            var errors = await rule.ValidateAsync(pastDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be in the future.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Greater_Int()
        {
            // Arrange
            var rule = new InFutureValidationRule<int>(() => 10);
            var value = 11;

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Smaller_Int()
        {
            // Arrange
            var rule = new InFutureValidationRule<int>(() => 10);
            var value = 9;

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be in the future.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message()
        {
            // Arrange
            var rule = new InFutureValidationRule<DateTime>(() => DateTime.Now, "Date must be later.");
            var pastDateTime = DateTime.Now.AddDays(-1);

            // Act
            var errors = await rule.ValidateAsync(pastDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Date must be later.", errors.First());
        }
    }
}