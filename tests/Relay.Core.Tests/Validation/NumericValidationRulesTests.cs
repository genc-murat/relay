using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class NumericValidationRulesTests
    {
        [Fact]
        public async Task ZeroValidationRule_Should_Validate_Zero_Values()
        {
            // Arrange
            var rule = new ZeroValidationRule<int>();

            // Act & Assert
            var validResult = await rule.ValidateAsync(0);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(5);
            Assert.Single(invalidResult);
            Assert.Contains("must be zero", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task EvenValidationRule_Should_Validate_Even_Numbers()
        {
            // Arrange
            var rule = new EvenValidationRule();

            // Act & Assert
            var validResult = await rule.ValidateAsync(4);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(5);
            Assert.Single(invalidResult);
            Assert.Contains("must be even", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task BetweenValidationRule_Should_Validate_Range()
        {
            // Arrange
            var rule = new BetweenValidationRule<int>(10, 20);

            // Act & Assert
            var validResult = await rule.ValidateAsync(15);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(25);
            Assert.Single(invalidResult);
            Assert.Contains("must be between 10 and 20", invalidResult.First().ToLower());
        }

        [Fact]
        public void BetweenValidationRule_Constructor_Should_Throw_When_MinValue_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BetweenValidationRule<string>(null, "max"));
        }

        [Fact]
        public void BetweenValidationRule_Constructor_Should_Throw_When_MaxValue_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BetweenValidationRule<string>("min", null));
        }

        [Fact]
        public void BetweenValidationRule_Constructor_Should_Throw_When_MinValue_Greater_Than_MaxValue()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new BetweenValidationRule<int>(20, 10));
        }

        [Fact]
        public async Task BetweenValidationRule_Should_Validate_Boundary_Values()
        {
            // Arrange
            var rule = new BetweenValidationRule<int>(10, 20);

            // Act & Assert
            var minBoundaryResult = await rule.ValidateAsync(10);
            Assert.Empty(minBoundaryResult);

            var maxBoundaryResult = await rule.ValidateAsync(20);
            Assert.Empty(maxBoundaryResult);

            var belowMinResult = await rule.ValidateAsync(9);
            Assert.Single(belowMinResult);
            Assert.Contains("must be between 10 and 20", belowMinResult.First().ToLower());

            var aboveMaxResult = await rule.ValidateAsync(21);
            Assert.Single(aboveMaxResult);
            Assert.Contains("must be between 10 and 20", aboveMaxResult.First().ToLower());
        }

        [Fact]
        public async Task BetweenValidationRule_Should_Handle_Null_Request()
        {
            // Arrange
            var rule = new BetweenValidationRule<string>("a", "z");

            // Act
            var result = await rule.ValidateAsync(null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task BetweenValidationRule_Should_Work_With_Strings()
        {
            // Arrange
            var rule = new BetweenValidationRule<string>("apple", "zebra");

            // Act & Assert
            var validResult = await rule.ValidateAsync("banana");
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync("zebraa");
            Assert.Single(invalidResult);
            Assert.Contains("must be between apple and zebra", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task BetweenValidationRule_Should_Work_With_DateTime()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 12, 31);
            var rule = new BetweenValidationRule<DateTime>(startDate, endDate);

            // Act & Assert
            var validResult = await rule.ValidateAsync(new DateTime(2023, 6, 15));
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(new DateTime(2024, 1, 1));
            Assert.Single(invalidResult);
            Assert.Contains("must be between", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task BetweenValidationRule_Should_Work_With_Decimal()
        {
            // Arrange
            var rule = new BetweenValidationRule<decimal>(10.5m, 20.5m);

            // Act & Assert
            var validResult = await rule.ValidateAsync(15.0m);
            Assert.Empty(validResult);

            var invalidResult = await rule.ValidateAsync(25.0m);
            Assert.Single(invalidResult);
            Assert.Contains("must be between 10.5 and 20.5", invalidResult.First().ToLower());
        }

        [Fact]
        public async Task BetweenValidationRule_Should_Handle_Cancellation_Token()
        {
            // Arrange
            var rule = new BetweenValidationRule<int>(10, 20);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await rule.ValidateAsync(15, cts.Token));
        }
    }
}