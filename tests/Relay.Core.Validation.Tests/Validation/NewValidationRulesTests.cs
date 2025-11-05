using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class NewValidationRulesTests
    {
        [Fact]
        public async Task ContainsValidationRuleString_Should_Validate_String_Contains_Substring()
        {
            // Arrange
            var rule = new ContainsValidationRuleString("test");

            // Act & Assert
            var validResult = await rule.ValidateAsync("this is a test string");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("this is a string");
            Assert.Single(invalidResult);
            Assert.Contains("must contain 'test'", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task ContainsValidationRule_Should_Validate_Collection_Contains_Item()
        {
            // Arrange
            var rule = new ContainsValidationRule<string>("requiredItem");
            var validList = new List<string> { "item1", "requiredItem", "item3" };
            var invalidList = new List<string> { "item1", "item2", "item3" };

            // Act & Assert
            var validResult = await rule.ValidateAsync(validList);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(invalidList);
            Assert.Single(invalidResult);
            Assert.Contains("must contain 'requireditem'", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task StartsWithValidationRule_Should_Validate_String_Starts_With_Prefix()
        {
            // Arrange
            var rule = new StartsWithValidationRule("prefix");

            // Act & Assert
            var validResult = await rule.ValidateAsync("prefix string");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("string prefix");
            Assert.Single(invalidResult);
            Assert.Contains("must start with 'prefix'", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task EndsWithValidationRule_Should_Validate_String_Ends_With_Suffix()
        {
            // Arrange
            var rule = new EndsWithValidationRule("suffix");

            // Act & Assert
            var validResult = await rule.ValidateAsync("string suffix");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("suffix string");
            Assert.Single(invalidResult);
            Assert.Contains("must end with 'suffix'", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task ExactLengthValidationRule_Should_Validate_Exact_String_Length()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(5);

            // Act & Assert
            var validResult = await rule.ValidateAsync("hello");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("hello world");
            Assert.Single(invalidResult);
            Assert.Contains("must be exactly 5 characters", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task PositiveValidationRule_Should_Validate_Positive_Numbers()
        {
            // Arrange
            var rule = new PositiveValidationRule<int>();

            // Act & Assert
            var validResult = await rule.ValidateAsync(5);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(-5);
            Assert.Single(invalidResult);
            Assert.Contains("must be positive", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task NegativeValidationRule_Should_Validate_Negative_Numbers()
        {
            // Arrange
            var rule = new NegativeValidationRule<int>();

            // Act & Assert
            var validResult = await rule.ValidateAsync(-5);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(5);
            Assert.Single(invalidResult);
            Assert.Contains("must be negative", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task FutureValidationRule_Should_Validate_Future_Dates()
        {
            // Arrange
            var rule = new FutureValidationRule();
            var futureDate = DateTime.Now.AddDays(1);
            var pastDate = DateTime.Now.AddDays(-1);

            // Act & Assert
            var validResult = await rule.ValidateAsync(futureDate);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(pastDate);
            Assert.Single(invalidResult);
            Assert.Contains("must be in the future", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task PastValidationRule_Should_Validate_Past_Dates()
        {
            // Arrange
            var rule = new PastValidationRule();
            var pastDate = DateTime.Now.AddDays(-1);
            var futureDate = DateTime.Now.AddDays(1);

            // Act & Assert
            var validResult = await rule.ValidateAsync(pastDate);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(futureDate);
            Assert.Single(invalidResult);
            Assert.Contains("must be in the past", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task MinCountValidationRule_Should_Validate_Minimum_Collection_Count()
        {
            // Arrange
            var rule = new MinCountValidationRule<string>(3);
            var validList = new List<string> { "a", "b", "c", "d" };
            var invalidList = new List<string> { "a", "b" };

            // Act & Assert
            var validResult = await rule.ValidateAsync(validList);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(invalidList);
            Assert.Single(invalidResult);
            Assert.Contains("must contain at least 3 items", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task MaxCountValidationRule_Should_Validate_Maximum_Collection_Count()
        {
            // Arrange
            var rule = new MaxCountValidationRule<string>(3);
            var validList = new List<string> { "a", "b", "c" };
            var invalidList = new List<string> { "a", "b", "c", "d" };

            // Act & Assert
            var validResult = await rule.ValidateAsync(validList);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(invalidList);
            Assert.Single(invalidResult);
            Assert.Contains("must contain at most 3 items", invalidResult.First().ToLower());
        }
    }
}