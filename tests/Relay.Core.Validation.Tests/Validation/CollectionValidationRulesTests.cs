using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class CollectionValidationRulesTests
    {
        [Fact]
        public async Task EmptyValidationRule_Should_Validate_Empty_Collections()
        {
            // Arrange
            var rule = new EmptyValidationRule<string>();
            var emptyList = new List<string>();
            var nonEmptyList = new List<string> { "item" };

            // Act & Assert
            var validResult = await rule.ValidateAsync(emptyList);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(nonEmptyList);
            Assert.Single(invalidResult);
            Assert.Contains("must be empty", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task UniqueValidationRule_Should_Validate_Unique_Items()
        {
            // Arrange
            var rule = new UniqueValidationRule<string>();
            var uniqueList = new List<string> { "a", "b", "c" };
            var duplicateList = new List<string> { "a", "b", "a" };

            // Act & Assert
            var validResult = await rule.ValidateAsync(uniqueList);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(duplicateList);
            Assert.Single(invalidResult);
            Assert.Contains("must contain unique items", invalidResult.First().ToLower());
        }
    }
}