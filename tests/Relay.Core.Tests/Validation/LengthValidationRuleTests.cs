using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class LengthValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Length_Is_In_Range()
        {
            // Arrange
            var rule = new LengthValidationRule(1, 10);
            var request = "hello";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_String_Is_Null()
        {
            // Arrange
            var rule = new LengthValidationRule(1, 10);
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Length_Is_Below_Minimum()
        {
            // Arrange
            var rule = new LengthValidationRule(3, 10);
            var request = "hi";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Length must be between 3 and 10 characters.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Length_Is_Above_Maximum()
        {
            // Arrange
            var rule = new LengthValidationRule(1, 5);
            var request = "hello world";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Length must be between 1 and 5 characters.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new LengthValidationRule(1, 5, "Custom length error");
            var request = "too long string";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom length error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Inclusive_Boundaries()
        {
            // Arrange
            var rule = new LengthValidationRule(1, 5);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("a"));
            Assert.Empty(await rule.ValidateAsync("hello"));
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new LengthValidationRule(1, 10);
            var request = "test";
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
            var rule = new LengthValidationRule(1, 5);
            var request = "too long";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}