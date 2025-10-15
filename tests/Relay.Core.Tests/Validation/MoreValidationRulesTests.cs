using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class MoreValidationRulesTests
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

        [Fact]
        public async Task ZeroValidationRule_Should_Validate_Zero_Values()
        {
            // Arrange
            var rule = new ZeroValidationRule<int>();

            // Act & Assert
            var validResult = await rule.ValidateAsync(0);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(5);
            Assert.Single(invalidResult);
            Assert.Contains("must be zero", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task EvenValidationRule_Should_Validate_Even_Numbers()
        {
            // Arrange
            var rule = new EvenValidationRule();

            // Act & Assert
            var validResult = await rule.ValidateAsync(4);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(5);
            Assert.Single(invalidResult);
            Assert.Contains("must be even", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task BetweenValidationRule_Should_Validate_Range()
        {
            // Arrange
            var rule = new BetweenValidationRule<int>(10, 20);

            // Act & Assert
            var validResult = await rule.ValidateAsync(15);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(25);
            Assert.Single(invalidResult);
            Assert.Contains("must be between 10 and 20", invalidResult.First().ToLower());
        }

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

        [Fact]
        public async Task AgeValidationRule_Should_Validate_Age_Range()
        {
            // Arrange
            var rule = new AgeValidationRule(18, 65);
            var age18 = 18;
            var age70 = 70;

            // Act & Assert
            var validResult = await rule.ValidateAsync(age18);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(age70);
            Assert.Single(invalidResult);
            Assert.Contains("age cannot exceed 65 years", invalidResult.First().ToLower());
        }

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