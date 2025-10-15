using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    public class EnumValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Enum_Value_Is_Valid()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>();
            var request = "Value1";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Enum_Value_Is_Null()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Enum_Value_Is_Invalid()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>();
            var request = "InvalidValue";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid value for enum TestEnum.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Case_Insensitive_When_IgnoreCase_Is_True()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>(ignoreCase: true);
            var request = "value1";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Case_Sensitive_When_IgnoreCase_Is_False()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>(ignoreCase: false);
            var request = "value1";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid value for enum TestEnum.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>(errorMessage: "Custom enum error");
            var request = "invalid";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom enum error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>();
            var request = "Value2";
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
            var rule = new EnumValidationRule<TestEnum>();
            var request = "invalid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}