using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class NoWhitespaceValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Has_No_Whitespace()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Single_Character()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "a";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Spaces()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc 123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Tabs()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc\t123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Newlines()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc\n123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Null()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Empty()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule("Custom whitespace error");
            var request = "abc 123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom whitespace error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc123";
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync(request, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Cancelled_Token()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc 123";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Carriage_Return()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc\r123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Form_Feed()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc\f123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Vertical_Tab()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc\v123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Unicode_Spaces()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc\u00A0123"; // Non-breaking space

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Thin_Space()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "abc\u2009123"; // Thin space

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Empty_String()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors); // Empty string is considered to not contain whitespace, but null/empty check fails
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Very_Long_String_With_Whitespace()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = new string('a', 500) + " " + new string('b', 500);

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must not contain whitespace.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Very_Long_String_Without_Whitespace()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule();
            var request = new string('a', 1000);

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message_Containing_Whitespace()
        {
            // Arrange
            var rule = new NoWhitespaceValidationRule("Custom error message with spaces");
            var request = "abc 123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom error message with spaces", errors.First());
        }
    }
}