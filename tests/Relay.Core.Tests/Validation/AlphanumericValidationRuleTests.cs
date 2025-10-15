using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class AlphanumericValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Alphanumeric()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abc123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Only_Letters()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abc";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Only_Digits()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Special_Characters()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abc@123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Spaces()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abc 123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Null()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Empty()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new AlphanumericValidationRule("Custom alphanumeric error");
            var request = "abc@123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom alphanumeric error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
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
            var rule = new AlphanumericValidationRule();
            var request = "abc@123";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Mixed_Case_Alphanumeric()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "AbC123XyZ";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Underscore()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abc_123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Hyphen()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abc-123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Period()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abc.123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Newlines()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abc\n123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Tabs()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abc\t123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Only_Special_Characters()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "!@#$%^&*()";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must contain only letters and numbers.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Unicode_Letters()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = "abcçğıöşü123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Very_Long_Alphanumeric_String()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();
            var request = new string('a', 500) + new string('1', 500);

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Single_Character_Alphanumeric()
        {
            // Arrange
            var rule = new AlphanumericValidationRule();

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("a"));
            Assert.Empty(await rule.ValidateAsync("1"));
            Assert.Single(await rule.ValidateAsync("@"));
        }
    }
}