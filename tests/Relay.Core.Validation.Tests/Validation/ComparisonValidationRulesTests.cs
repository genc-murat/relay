using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class ComparisonValidationRulesTests
    {
        [Fact]
        public async Task IsInValidationRule_Should_Validate_Value_In_List()
        {
            // Arrange
            var rule = new IsInValidationRule<string>("option1", "option2", "option3");

            // Act & Assert
            var validResult = await rule.ValidateAsync("option2");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("option4");
            Assert.Single(invalidResult);
            Assert.Contains("must be one of", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task NotInValidationRule_Should_Validate_Value_Not_In_List()
        {
            // Arrange
            var rule = new NotInValidationRule<string>("forbidden1", "forbidden2");

            // Act & Assert
            var validResult = await rule.ValidateAsync("allowed");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("forbidden1");
            Assert.Single(invalidResult);
            Assert.Contains("must not be one of", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task IsEqualValidationRule_Should_Validate_Equality()
        {
            // Arrange
            var rule = new IsEqualValidationRule<string>("expected");

            // Act & Assert
            var validResult = await rule.ValidateAsync("expected");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("different");
            Assert.Single(invalidResult);
            Assert.Contains("must equal 'expected'", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task NotEqualValidationRule_Should_Validate_Inequality()
        {
            // Arrange
            var rule = new NotEqualValidationRule<string>("forbidden");

            // Act & Assert
            var validResult = await rule.ValidateAsync("allowed");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("forbidden");
            Assert.Single(invalidResult);
            Assert.Contains("must not equal 'forbidden'", invalidResult.First().ToLower());
        }
    }
}