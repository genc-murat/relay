using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class CreditCardValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Credit_Card_Is_Valid()
        {
            // Arrange
            var rule = new CreditCardValidationRule();
            var request = "4532015112830366"; // Valid Visa test number

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Credit_Card_Is_Invalid()
        {
            // Arrange
            var rule = new CreditCardValidationRule();
            var request = "4532015112830367"; // Invalid checksum

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid credit card number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Credit_Card_Is_Too_Short()
        {
            // Arrange
            var rule = new CreditCardValidationRule();
            var request = "123456789"; // Too short

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid credit card number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Credit_Card_Is_Too_Long()
        {
            // Arrange
            var rule = new CreditCardValidationRule();
            var request = "123456789012345678901234567890"; // Too long

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid credit card number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Ignore_Non_Digit_Characters()
        {
            // Arrange
            var rule = new CreditCardValidationRule();
            var request = "4532-0151-1283-0366"; // Valid with dashes

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new CreditCardValidationRule("Custom credit card error");
            var request = "invalid";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom credit card error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Credit_Card_Is_Null()
        {
            // Arrange
            var rule = new CreditCardValidationRule();
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
            var rule = new CreditCardValidationRule();
            var request = "4532015112830366";
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
            var rule = new CreditCardValidationRule();
            var request = "invalid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}