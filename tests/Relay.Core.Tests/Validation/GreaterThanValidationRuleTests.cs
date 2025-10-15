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

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Negative_Numbers()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<int>(-10);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(-5));
            Assert.Single(await rule.ValidateAsync(-15));
            Assert.Single(await rule.ValidateAsync(-10));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Zero_Boundary()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<int>(0);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(1));
            Assert.Single(await rule.ValidateAsync(0));
            Assert.Single(await rule.ValidateAsync(-1));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Large_Numbers()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<long>(1000000000L);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(1000000001L));
            Assert.Single(await rule.ValidateAsync(1000000000L));
            Assert.Single(await rule.ValidateAsync(999999999L));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Decimal_Numbers()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<decimal>(10.5m);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(10.6m));
            Assert.Single(await rule.ValidateAsync(10.5m));
            Assert.Single(await rule.ValidateAsync(10.4m));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Double_Numbers()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<double>(1.5);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(1.6));
            Assert.Single(await rule.ValidateAsync(1.5));
            Assert.Single(await rule.ValidateAsync(1.4));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Float_Numbers()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<float>(2.5f);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(2.6f));
            Assert.Single(await rule.ValidateAsync(2.5f));
            Assert.Single(await rule.ValidateAsync(2.4f));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message_Containing_Numbers()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<int>(5, "Value must be > 5, got {0}");
            var request = 3;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be > 5, got {0}", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Byte_Type()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<byte>(10);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync((byte)15));
            Assert.Single(await rule.ValidateAsync((byte)10));
            Assert.Single(await rule.ValidateAsync((byte)5));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Short_Type()
        {
            // Arrange
            var rule = new GreaterThanValidationRule<short>(100);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync((short)150));
            Assert.Single(await rule.ValidateAsync((short)100));
            Assert.Single(await rule.ValidateAsync((short)50));
        }
    }
}