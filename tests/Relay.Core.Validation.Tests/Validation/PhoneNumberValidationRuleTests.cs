using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class PhoneNumberValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Phone_Is_Valid()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();
            var request = "+1-555-123-4567";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Phone_Is_Null()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Phone_Is_Invalid()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();
            var request = "invalid-phone";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid phone number format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Pattern()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule(@"^\d{10}$", "Must be 10 digits");
            var request = "1234567890";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule(errorMessage: "Custom phone error");
            var request = "abc";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom phone error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();
            var request = "555-1234";
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
            var rule = new PhoneNumberValidationRule();
            var request = "invalid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Theory]
        [InlineData("+1 555 123 4567")]
        [InlineData("(555) 123-4567")]
        [InlineData("555.123.4567")]
        [InlineData("+44 20 7946 0958")] // UK
        [InlineData("+91 9876543210")] // India
        [InlineData("0123456789")] // 10 digits no +
        [InlineData("123456789012345")] // Max length 15
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_Phone_Formats(string phone)
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();

            // Act
            var errors = await rule.ValidateAsync(phone);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("123456")] // Too short (6 chars)
        [InlineData("1234567890123456")] // Too long (16 chars)
        [InlineData("abc1234567")] // Invalid chars
        [InlineData("++123456789")] // Multiple +
        [InlineData("+1234567890123456")] // + plus 15 digits = 16 chars

        [InlineData("()5551234567")] // Empty parentheses
        [InlineData("555  123  4567")] // Multiple spaces
        public async Task ValidateAsync_Should_Return_Error_For_Invalid_Phone_Formats(string phone)
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();

            // Act
            var errors = await rule.ValidateAsync(phone);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid phone number format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Exactly_7_Digits()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();
            var request = "1234567";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Exactly_15_Digits_With_Separators()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();
            var request = "+1-234-567-89012"; // 15 chars

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_16_Characters()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();
            var request = "1234567890123456"; // 16 digits

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid phone number format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Strict_10_Digit_Pattern()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule(@"^\d{10}$", "Must be exactly 10 digits");
            var validRequest = "1234567890";
            var invalidRequest = "123456789";

            // Act
            var validErrors = await rule.ValidateAsync(validRequest);
            var invalidErrors = await rule.ValidateAsync(invalidRequest);

            // Assert
            Assert.Empty(validErrors);
            Assert.Single(invalidErrors);
            Assert.Equal("Must be exactly 10 digits", invalidErrors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Leading_Trailing_Whitespace()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();
            var request = "  +1-555-123-4567  ";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors); // Whitespace is allowed in the pattern
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Phone_With_Invalid_Characters()
        {
            // Arrange
            var rule = new PhoneNumberValidationRule();
            var request = "555-123-4567!";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid phone number format.", errors.First());
        }
    }
}