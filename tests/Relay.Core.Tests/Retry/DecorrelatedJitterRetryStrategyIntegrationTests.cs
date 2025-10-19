using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Relay.Core.Retry;
using Xunit;

namespace Relay.Core.Tests.Retry
{
    /// <summary>
    /// Integration and edge case tests for DecorrelatedJitterRetryStrategy.
    /// </summary>
    public class DecorrelatedJitterRetryStrategyIntegrationTests
    {
        #region Integration and Edge Case Tests

        [Fact]
        public async Task Strategy_Should_WorkCorrectly_WithVerySmallDelays()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromMilliseconds(10));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay1 = await strategy.GetRetryDelayAsync(1, exception);
            var delay2 = await strategy.GetRetryDelayAsync(2, exception);

            // Assert
            Assert.InRange(delay1, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(10));
            Assert.InRange(delay2, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(10));
        }

        [Fact]
        public async Task Strategy_Should_WorkCorrectly_WithVeryLargeDelays()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMinutes(1),
                TimeSpan.FromHours(1));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay1 = await strategy.GetRetryDelayAsync(1, exception);
            var delay2 = await strategy.GetRetryDelayAsync(2, exception);

            // Assert
            Assert.InRange(delay1, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
            Assert.InRange(delay2, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
        }

        [Fact]
        public async Task Strategy_Should_WorkCorrectly_WithZeroBaseDelay()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay1 = await strategy.GetRetryDelayAsync(1, exception);
            var delay2 = await strategy.GetRetryDelayAsync(2, exception);

            // Assert
            Assert.InRange(delay1, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            Assert.InRange(delay2, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task Strategy_Should_HandleHighAttemptNumbers()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay100 = await strategy.GetRetryDelayAsync(100, exception);
            var delay1000 = await strategy.GetRetryDelayAsync(1000, exception);

            // Assert - Should still respect bounds
            Assert.InRange(delay100, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30));
            Assert.InRange(delay1000, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task Strategy_Should_WorkCorrectly_WithVariousMaxAttempts(int maxAttempts)
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30),
                maxAttempts: maxAttempts);
            var exception = new InvalidOperationException("Test exception");

            // Act & Assert
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var shouldRetry = await strategy.ShouldRetryAsync(attempt, exception);
                Assert.True(shouldRetry, $"attempt {attempt} is within maxAttempts {maxAttempts}");
            }

            var shouldNotRetry = await strategy.ShouldRetryAsync(maxAttempts + 1, exception);
            Assert.False(shouldNotRetry, $"attempt {maxAttempts + 1} exceeds maxAttempts {maxAttempts}");
        }

        #endregion
    }
}