using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class PasswordStrengthValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Password_Meets_All_Requirements()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule();
            var request = "StrongPass123!";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Password_Is_Too_Short()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule(minLength: 10);
            var request = "Short123!";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Password must contain at least 10 characters, an uppercase letter, a lowercase letter, a digit, a special character.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Password_Misses_Uppercase()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule(requireUppercase: true);
            var request = "password123!";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Password must contain at least 8 characters, an uppercase letter, a lowercase letter, a digit, a special character.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Password_Misses_Lowercase()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule(requireLowercase: true);
            var request = "PASSWORD123!";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Password must contain at least 8 characters, an uppercase letter, a lowercase letter, a digit, a special character.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Password_Misses_Digit()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule(requireDigit: true);
            var request = "Password!";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Password must contain at least 8 characters, an uppercase letter, a lowercase letter, a digit, a special character.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Password_Misses_Special_Char()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule(requireSpecialChar: true);
            var request = "Password123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Password must contain at least 8 characters, an uppercase letter, a lowercase letter, a digit, a special character.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Requirements()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule(
                minLength: 6,
                requireUppercase: false,
                requireLowercase: false,
                requireDigit: true,
                requireSpecialChar: false);
            var request = "pass12";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule(errorMessage: "Custom password error");
            var request = "weak";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom password error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Password_Is_Null()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new PasswordStrengthValidationRule();
            var request = "StrongPass123!";
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
            var rule = new PasswordStrengthValidationRule();
            var request = "weak";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}