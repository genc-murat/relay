using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class RangeValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_In_Range()
        {
            // Arrange
            var rule = new RangeValidationRule<int>(1, 10);
            var request = 5;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Below_Minimum()
        {
            // Arrange
            var rule = new RangeValidationRule<int>(1, 10);
            var request = 0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be between 1 and 10.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Above_Maximum()
        {
            // Arrange
            var rule = new RangeValidationRule<int>(1, 10);
            var request = 15;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be between 1 and 10.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new RangeValidationRule<double>(1.0, 5.0, "Custom error message");
            var request = 10.0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom error message", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Inclusive_Boundaries()
        {
            // Arrange
            var rule = new RangeValidationRule<int>(1, 10);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(1));
            Assert.Empty(await rule.ValidateAsync(10));
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new RangeValidationRule<int>(1, 10);
            var request = 5;
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
            var rule = new RangeValidationRule<int>(1, 10);
            var request = 15;
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}