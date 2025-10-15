using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class PostalCodeValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_US_Postal_Code_Is_Valid()
        {
            // Arrange
            var rule = new PostalCodeValidationRule("US");
            var request = "12345";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_US_Postal_Code_With_Extension_Is_Valid()
        {
            // Arrange
            var rule = new PostalCodeValidationRule("US");
            var request = "12345-6789";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_US_Postal_Code_Is_Invalid()
        {
            // Arrange
            var rule = new PostalCodeValidationRule("US");
            var request = "1234";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid postal code format for US.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_CA_Postal_Code_Is_Valid()
        {
            // Arrange
            var rule = new PostalCodeValidationRule("CA");
            var request = "K1A 0A6";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new PostalCodeValidationRule("US", "Custom postal error");
            var request = "invalid";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom postal error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Postal_Code_Is_Null()
        {
            // Arrange
            var rule = new PostalCodeValidationRule();
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
            var rule = new PostalCodeValidationRule();
            var request = "12345";
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
            var rule = new PostalCodeValidationRule();
            var request = "invalid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}