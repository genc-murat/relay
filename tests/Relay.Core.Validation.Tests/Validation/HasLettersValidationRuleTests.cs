using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class HasLettersValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Contains_Letters()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "abc";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_All_Letters()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "HelloWorld";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Contains_Letters_And_Numbers()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "abc123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Contains_Letters_And_Special_Characters()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "hello@world.com";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Contains_Only_Whitespace_And_Letters()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "  hello  ";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Single_Letter()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "a";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Contains_Unicode_Letters()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "héllo";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Only_Numbers()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "123456";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain at least one letter.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Only_Special_Characters()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "!@#$%^&*()";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain at least one letter.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Only_Whitespace()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "   \t\n  ";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain at least one letter.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Empty_String()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = string.Empty;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Null()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Only_Numbers_And_Special_Chars()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "123!@#";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain at least one letter.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_CancellationToken()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "abc";
            var cancellationToken = new CancellationToken(true);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                rule.ValidateAsync(request, cancellationToken).AsTask());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Contains_Mixed_Case_Letters()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "AbCdEf";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Contains_Letters_From_Different_Alphabets()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = "HelloПривет";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Very_Long_Without_Letters()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = new string('1', 1000) + new string('!', 1000);

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain at least one letter.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Very_Long_With_Letters()
        {
            // Arrange
            var rule = new HasLettersValidationRule();
            var request = new string('a', 1000) + new string('1', 1000);

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }
    }
}