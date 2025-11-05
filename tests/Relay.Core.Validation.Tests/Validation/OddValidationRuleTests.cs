using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class OddValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Empty_Errors_When_Value_Is_Odd_Positive()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = 5;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Empty_Errors_When_Value_Is_Odd_Negative()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = -7;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Error_When_Value_Is_Even_Positive()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = 4;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Error_When_Value_Is_Even_Negative()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = -6;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = 0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Empty_Errors_When_Value_Is_Int_MaxValue()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = int.MaxValue; // 2147483647 (odd)

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Error_When_Value_Is_Int_MaxValue_Minus_One()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = int.MaxValue - 1; // 2147483646 (even)

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Empty_Errors_When_Value_Is_Int_MinValue()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = int.MinValue; // -2147483648 (even, but let's check)

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Error_When_Value_Is_Int_MinValue_Plus_One()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = int.MinValue + 1; // -2147483647 (odd)

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Handle_CancellationToken()
        {
            // Arrange
            var rule = new OddValidationRule();
            var request = 5;
            var cancellationToken = new CancellationToken(true);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                rule.ValidateAsync(request, cancellationToken).AsTask());
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Empty_Errors_When_Value_Is_Odd_Positive()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = 123456789L;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Empty_Errors_When_Value_Is_Odd_Negative()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = -987654321L;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Error_When_Value_Is_Even_Positive()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = 123456788L;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Error_When_Value_Is_Even_Negative()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = -987654320L;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Error_When_Value_Is_Zero()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = 0L;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Empty_Errors_When_Value_Is_Long_MaxValue()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = long.MaxValue; // 9223372036854775807 (odd)

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Error_When_Value_Is_Long_MaxValue_Minus_One()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = long.MaxValue - 1; // 9223372036854775806 (even)

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Error_When_Value_Is_Long_MinValue()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = long.MinValue; // -9223372036854775808 (even)

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be odd.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Empty_Errors_When_Value_Is_Long_MinValue_Plus_One()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = long.MinValue + 1; // -9223372036854775807 (odd)

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Handle_CancellationToken()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var request = 123456789L;
            var cancellationToken = new CancellationToken(true);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                rule.ValidateAsync(request, cancellationToken).AsTask());
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Empty_Errors_For_Various_Odd_Numbers()
        {
            // Arrange
            var rule = new OddValidationRule();
            var oddNumbers = new[] { 1, 3, 7, 9, 11, 13, 15, 17, 19, 21 };

            foreach (var number in oddNumbers)
            {
                // Act
                var errors = await rule.ValidateAsync(number);

                // Assert
                Assert.Empty(errors);
            }
        }

        [Fact]
        public async Task ValidateAsync_Int_Should_Return_Error_For_Various_Even_Numbers()
        {
            // Arrange
            var rule = new OddValidationRule();
            var evenNumbers = new[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };

            foreach (var number in evenNumbers)
            {
                // Act
                var errors = await rule.ValidateAsync(number);

                // Assert
                Assert.Single(errors);
                Assert.Equal("Value must be odd.", errors.First());
            }
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Empty_Errors_For_Various_Odd_Numbers()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var oddNumbers = new[] { 1L, 3L, 7L, 9L, 11L, 13L, 15L, 17L, 19L, 21L };

            foreach (var number in oddNumbers)
            {
                // Act
                var errors = await rule.ValidateAsync(number);

                // Assert
                Assert.Empty(errors);
            }
        }

        [Fact]
        public async Task ValidateAsync_Long_Should_Return_Error_For_Various_Even_Numbers()
        {
            // Arrange
            var rule = new OddValidationRuleLong();
            var evenNumbers = new[] { 2L, 4L, 6L, 8L, 10L, 12L, 14L, 16L, 18L, 20L };

            foreach (var number in evenNumbers)
            {
                // Act
                var errors = await rule.ValidateAsync(number);

                // Assert
                Assert.Single(errors);
                Assert.Equal("Value must be odd.", errors.First());
            }
        }
    }
}