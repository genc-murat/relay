using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class BooleanValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Expected_True()
        {
            // Arrange
            var rule = new BooleanValidationRule(true);

            // Act
            var errors = await rule.ValidateAsync(true);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Expected_False()
        {
            // Arrange
            var rule = new BooleanValidationRule(false);

            // Act
            var errors = await rule.ValidateAsync(false);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Not_Expected_True()
        {
            // Arrange
            var rule = new BooleanValidationRule(true);

            // Act
            var errors = await rule.ValidateAsync(false);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be True.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Not_Expected_False()
        {
            // Arrange
            var rule = new BooleanValidationRule(false);

            // Act
            var errors = await rule.ValidateAsync(true);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be False.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message()
        {
            // Arrange
            var rule = new BooleanValidationRule(true, "Must be checked.");

            // Act
            var errors = await rule.ValidateAsync(false);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Must be checked.", errors.First());
        }
    }
}