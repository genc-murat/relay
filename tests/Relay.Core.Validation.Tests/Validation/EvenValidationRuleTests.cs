using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class EvenValidationRuleTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(-2)]
        [InlineData(-4)]
        [InlineData(-6)]
        [InlineData(-100)]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Even(int value)
        {
            // Arrange
            var rule = new EvenValidationRule();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(11)]
        [InlineData(101)]
        [InlineData(1001)]
        [InlineData(-1)]
        [InlineData(-3)]
        [InlineData(-5)]
        [InlineData(-99)]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Odd(int value)
        {
            // Arrange
            var rule = new EvenValidationRule();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be even.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Zero()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var request = 0;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_One()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var request = 1;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be even.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Negative_Even_Numbers()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var request = -8;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Negative_Odd_Numbers()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var request = -9;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be even.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Large_Even_Numbers()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var request = int.MaxValue - 1; // Largest even number less than MaxValue

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Large_Odd_Numbers()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var request = int.MaxValue; // Largest odd number

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be even.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Smallest_Even_Number()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var request = int.MinValue; // MinValue is even

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Smallest_Odd_Number()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var request = int.MinValue + 1; // Smallest odd number

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be even.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync(4, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Cancelled_Token()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(5, cts.Token));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(12)]
        [InlineData(14)]
        [InlineData(16)]
        [InlineData(18)]
        [InlineData(20)]
        public async Task ValidateAsync_Should_Work_With_Positive_Even_Numbers(int value)
        {
            // Arrange
            var rule = new EvenValidationRule();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-4)]
        [InlineData(-6)]
        [InlineData(-8)]
        [InlineData(-10)]
        [InlineData(-12)]
        [InlineData(-14)]
        [InlineData(-16)]
        [InlineData(-18)]
        [InlineData(-20)]
        public async Task ValidateAsync_Should_Work_With_Negative_Even_Numbers(int value)
        {
            // Arrange
            var rule = new EvenValidationRule();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(11)]
        [InlineData(13)]
        [InlineData(15)]
        [InlineData(17)]
        [InlineData(19)]
        public async Task ValidateAsync_Should_Work_With_Positive_Odd_Numbers(int value)
        {
            // Arrange
            var rule = new EvenValidationRule();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be even.", errors.First());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-3)]
        [InlineData(-5)]
        [InlineData(-7)]
        [InlineData(-9)]
        [InlineData(-11)]
        [InlineData(-13)]
        [InlineData(-15)]
        [InlineData(-17)]
        [InlineData(-19)]
        public async Task ValidateAsync_Should_Work_With_Negative_Odd_Numbers(int value)
        {
            // Arrange
            var rule = new EvenValidationRule();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be even.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Int32_Boundary_Values()
        {
            // Arrange
            var rule = new EvenValidationRule();

            // Test boundary values
            var errorsMin = await rule.ValidateAsync(int.MinValue);
            var errorsMax = await rule.ValidateAsync(int.MaxValue);

            // Assert
            Assert.Empty(errorsMin); // MinValue (-2147483648) is even
            Assert.Single(errorsMax); // MaxValue (2147483647) is odd
            Assert.Equal("Value must be even.", errorsMax.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Be_Consistent_With_Modulo_Operation()
        {
            // Arrange
            var rule = new EvenValidationRule();
            var testValues = new[] { -10, -5, -2, -1, 0, 1, 2, 5, 10 };

            foreach (var value in testValues)
            {
                // Act
                var errors = await rule.ValidateAsync(value);
                var isEvenByModulo = value % 2 == 0;

                // Assert
                if (isEvenByModulo)
                {
                    Assert.Empty(errors);
                }
                else
                {
                    Assert.Single(errors);
                    Assert.Equal("Value must be even.", errors.First());
                }
            }
        }
    }
}