using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class StringValidationRulesTests
    {
        [Fact]
        public async Task IsEmptyValidationRule_Should_Validate_Empty_Strings()
        {
            // Arrange
            var rule = new IsEmptyValidationRule();

            // Act & Assert
            var validResult = await rule.ValidateAsync("");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("not empty");
            Assert.Single(invalidResult);
            Assert.Contains("must be empty", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task IsUpperCaseValidationRule_Should_Validate_Uppercase_Strings()
        {
            // Arrange
            var rule = new IsUpperCaseValidationRule();

            // Act & Assert
            var validResult = await rule.ValidateAsync("HELLO");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("Hello");
            Assert.Single(invalidResult);
            Assert.Contains("must consist only of uppercase", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task HasDigitsValidationRule_Should_Validate_Digit_Presence()
        {
            // Arrange
            var rule = new HasDigitsValidationRule();

            // Act & Assert
            var validResult = await rule.ValidateAsync("abc123");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("abcdef");
            Assert.Single(invalidResult);
            Assert.Contains("must contain at least one digit", invalidResult.First().ToLower());
        }
    }
}