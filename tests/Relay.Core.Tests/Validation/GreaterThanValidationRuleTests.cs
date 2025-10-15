using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class GreaterThanValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Greater_Than_Minimum()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<int>(5);
            var request = 10;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Equal_To_Minimum()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<int>(5);
            var request = 5;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be greater than 5.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Less_Than_Minimum()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<int>(5);
            var request = 3;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be greater than 5.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<double>(1.5, "Custom greater than error");
            var request = 1.0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom greater than error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Different_Types()
        {
            // Arrange
            var intRule = new GreaterThanValidationRule<int>(0);
            var doubleRule = new GreaterThanValidationRule<double>(0.0);
            var decimalRule = new GreaterThanValidationRule<decimal>(0.0m);

            // Act & Assert
            Assert.Empty(await intRule.ValidateAsync(1));
            Assert.Empty(await doubleRule.ValidateAsync(1.5));
            Assert.Empty(await decimalRule.ValidateAsync(1.5m));
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<int>(5);
            var request = 10;
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
            var rule = new GreaterThanValidationRule<int>(5);
            var request = 3;
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}