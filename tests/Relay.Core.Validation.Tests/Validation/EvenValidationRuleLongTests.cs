using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class EvenValidationRuleLongTests
    {
        [Theory]
        [InlineData(0L)]
        [InlineData(2L)]
        [InlineData(4L)]
        [InlineData(6L)]
        [InlineData(8L)]
        [InlineData(10L)]
        [InlineData(100L)]
        [InlineData(1000L)]
        [InlineData(-2L)]
        [InlineData(-4L)]
        [InlineData(-6L)]
        [InlineData(-100L)]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Even(long value)
        {
            // Arrange
            var rule = new EvenValidationRuleLong();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(3L)]
        [InlineData(5L)]
        [InlineData(7L)]
        [InlineData(9L)]
        [InlineData(11L)]
        [InlineData(101L)]
        [InlineData(1001L)]
        [InlineData(-1L)]
        [InlineData(-3L)]
        [InlineData(-5L)]
        [InlineData(-99L)]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Odd(long value)
        {
            // Arrange
            var rule = new EvenValidationRuleLong();

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
            var rule = new EvenValidationRuleLong();
            var request = 0L;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_One()
        {
            // Arrange
            var rule = new EvenValidationRuleLong();
            var request = 1L;

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
            var rule = new EvenValidationRuleLong();
            var request = -8L;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Negative_Odd_Numbers()
        {
            // Arrange
            var rule = new EvenValidationRuleLong();
            var request = -9L;

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
            var rule = new EvenValidationRuleLong();
            var request = long.MaxValue - 1; // Largest even number less than MaxValue

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Large_Odd_Numbers()
        {
            // Arrange
            var rule = new EvenValidationRuleLong();
            var request = long.MaxValue; // Largest odd number

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
            var rule = new EvenValidationRuleLong();
            var request = long.MinValue; // MinValue is even

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Smallest_Odd_Number()
        {
            // Arrange
            var rule = new EvenValidationRuleLong();
            var request = long.MinValue + 1; // Smallest odd number

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
            var rule = new EvenValidationRuleLong();
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync(4L, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Cancelled_Token()
        {
            // Arrange
            var rule = new EvenValidationRuleLong();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(5L, cts.Token));
        }

        [Theory]
        [InlineData(2L)]
        [InlineData(4L)]
        [InlineData(6L)]
        [InlineData(8L)]
        [InlineData(10L)]
        [InlineData(12L)]
        [InlineData(14L)]
        [InlineData(16L)]
        [InlineData(18L)]
        [InlineData(20L)]
        public async Task ValidateAsync_Should_Work_With_Positive_Even_Numbers(long value)
        {
            // Arrange
            var rule = new EvenValidationRuleLong();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(-2L)]
        [InlineData(-4L)]
        [InlineData(-6L)]
        [InlineData(-8L)]
        [InlineData(-10L)]
        [InlineData(-12L)]
        [InlineData(-14L)]
        [InlineData(-16L)]
        [InlineData(-18L)]
        [InlineData(-20L)]
        public async Task ValidateAsync_Should_Work_With_Negative_Even_Numbers(long value)
        {
            // Arrange
            var rule = new EvenValidationRuleLong();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(3L)]
        [InlineData(5L)]
        [InlineData(7L)]
        [InlineData(9L)]
        [InlineData(11L)]
        [InlineData(13L)]
        [InlineData(15L)]
        [InlineData(17L)]
        [InlineData(19L)]
        public async Task ValidateAsync_Should_Work_With_Positive_Odd_Numbers(long value)
        {
            // Arrange
            var rule = new EvenValidationRuleLong();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be even.", errors.First());
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData(-3L)]
        [InlineData(-5L)]
        [InlineData(-7L)]
        [InlineData(-9L)]
        [InlineData(-11L)]
        [InlineData(-13L)]
        [InlineData(-15L)]
        [InlineData(-17L)]
        [InlineData(-19L)]
        public async Task ValidateAsync_Should_Work_With_Negative_Odd_Numbers(long value)
        {
            // Arrange
            var rule = new EvenValidationRuleLong();

            // Act
            var errors = await rule.ValidateAsync(value);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be even.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Int64_Boundary_Values()
        {
            // Arrange
            var rule = new EvenValidationRuleLong();

            // Test boundary values
            var errorsMin = await rule.ValidateAsync(long.MinValue);
            var errorsMax = await rule.ValidateAsync(long.MaxValue);

            // Assert
            Assert.Empty(errorsMin); // MinValue (-9223372036854775808) is even
            Assert.Single(errorsMax); // MaxValue (9223372036854775807) is odd
            Assert.Equal("Value must be even.", errorsMax.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Be_Consistent_With_Modulo_Operation()
        {
            // Arrange
            var rule = new EvenValidationRuleLong();
            var testValues = new[] { -10L, -5L, -2L, -1L, 0L, 1L, 2L, 5L, 10L };

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

        [Fact]
        public async Task ValidateAsync_Should_Handle_Very_Large_Numbers()
        {
            // Arrange
            var rule = new EvenValidationRuleLong();
            var largeEven = 9223372036854775806L; // long.MaxValue - 1
            var largeOdd = 9223372036854775807L;   // long.MaxValue

            // Act
            var errorsEven = await rule.ValidateAsync(largeEven);
            var errorsOdd = await rule.ValidateAsync(largeOdd);

            // Assert
            Assert.Empty(errorsEven);
            Assert.Single(errorsOdd);
            Assert.Equal("Value must be even.", errorsOdd.First());
        }
    }
}