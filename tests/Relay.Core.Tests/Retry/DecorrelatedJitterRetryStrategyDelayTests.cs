using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Relay.Core.Retry;
using Relay.Core.Retry.Strategies;
using Xunit;

namespace Relay.Core.Tests.Retry
{
    /// <summary>
    /// Unit tests for DecorrelatedJitterRetryStrategy GetRetryDelayAsync functionality.
    /// </summary>
    public class DecorrelatedJitterRetryStrategyDelayTests
    {
        #region GetRetryDelayAsync Tests

        [Fact]
        public async Task GetRetryDelayAsync_Should_ReturnZero_ForAttemptZero()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay = await strategy.GetRetryDelayAsync(0, exception);

            // Assert
            Assert.Equal(TimeSpan.Zero, delay);
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_ReturnZero_ForNegativeAttempt()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay = await strategy.GetRetryDelayAsync(-1, exception);

            // Assert
            Assert.Equal(TimeSpan.Zero, delay);
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_ReturnDelayWithinRange_ForFirstAttempt()
        {
            // Arrange
            var baseDelay = TimeSpan.FromMilliseconds(100);
            var maxDelay = TimeSpan.FromSeconds(30);
            var strategy = new DecorrelatedJitterRetryStrategy(baseDelay, maxDelay);
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay = await strategy.GetRetryDelayAsync(1, exception);

            // Assert
            // First attempt: delay should be between baseDelay (100ms) and baseDelay * 3 (300ms)
            Assert.InRange(delay, baseDelay, TimeSpan.FromMilliseconds(300));
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_NeverExceedMaxDelay()
        {
            // Arrange
            var baseDelay = TimeSpan.FromMilliseconds(100);
            var maxDelay = TimeSpan.FromSeconds(5);
            var strategy = new DecorrelatedJitterRetryStrategy(baseDelay, maxDelay);
            var exception = new InvalidOperationException("Test exception");

            // Act - Test multiple attempts including high attempt numbers
            var delays = new List<TimeSpan>();
            for (int attempt = 1; attempt <= 20; attempt++)
            {
                var delay = await strategy.GetRetryDelayAsync(attempt, exception);
                delays.Add(delay);
            }

            // Assert
            foreach (var delay in delays)
            {
                Assert.InRange(delay, baseDelay, maxDelay);
            }
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_ReturnBaseDelay_WhenBaseEqualsMax()
        {
            // Arrange
            var delay = TimeSpan.FromSeconds(5);
            var strategy = new DecorrelatedJitterRetryStrategy(delay, delay);
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result1 = await strategy.GetRetryDelayAsync(1, exception);
            var result2 = await strategy.GetRetryDelayAsync(2, exception);
            var result5 = await strategy.GetRetryDelayAsync(5, exception);

            // Assert
            Assert.Equal(delay, result1);
            Assert.Equal(delay, result2);
            Assert.Equal(delay, result5);
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_ProduceDecorrelatedDelays()
        {
            // Arrange
            var baseDelay = TimeSpan.FromMilliseconds(100);
            var maxDelay = TimeSpan.FromSeconds(30);
            var strategy = new DecorrelatedJitterRetryStrategy(baseDelay, maxDelay);
            var exception = new InvalidOperationException("Test exception");

            // Act - Get delays for the same attempt multiple times
            var delays = new List<TimeSpan>();
            for (int i = 0; i < 50; i++)
            {
                var delay = await strategy.GetRetryDelayAsync(3, exception);
                delays.Add(delay);
            }

            // Assert - Delays should have variance (not all the same)
            var distinctDelays = delays.Distinct().Count();
            Assert.True(distinctDelays > 10, "decorrelated jitter should produce varied delays");

            // All delays should be within valid range
            foreach (var delay in delays)
            {
                Assert.InRange(delay, baseDelay, maxDelay);
            }
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_TendToIncrease_OverAttempts()
        {
            // Arrange
            var baseDelay = TimeSpan.FromMilliseconds(100);
            var maxDelay = TimeSpan.FromSeconds(30);
            var strategy = new DecorrelatedJitterRetryStrategy(baseDelay, maxDelay);
            var exception = new InvalidOperationException("Test exception");

            // Act - Get average delays for different attempt numbers
            var samplesPerAttempt = 100;
            var averageDelay1 = await GetAverageDelay(strategy, exception, 1, samplesPerAttempt);
            var averageDelay3 = await GetAverageDelay(strategy, exception, 3, samplesPerAttempt);
            var averageDelay5 = await GetAverageDelay(strategy, exception, 5, samplesPerAttempt);

            // Assert - Average delays should generally increase (with some tolerance for randomness)
            Assert.True(averageDelay3 > averageDelay1);
            Assert.True(averageDelay5 > averageDelay3);
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_RespectCancellationToken()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var cts = new CancellationTokenSource();
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay = await strategy.GetRetryDelayAsync(1, exception, cts.Token);

            // Assert
            Assert.True(delay > TimeSpan.Zero);
            Assert.False(cts.Token.IsCancellationRequested);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculates the average delay for a given attempt number over multiple samples.
        /// Used to test that delays tend to increase with attempt number.
        /// </summary>
        private static async Task<TimeSpan> GetAverageDelay(
            DecorrelatedJitterRetryStrategy strategy,
            Exception exception,
            int attempt,
            int samples)
        {
            var delays = new List<TimeSpan>();

            for (int i = 0; i < samples; i++)
            {
                var delay = await strategy.GetRetryDelayAsync(attempt, exception);
                delays.Add(delay);
            }

            var averageMilliseconds = delays.Average(d => d.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(averageMilliseconds);
        }

        #endregion
    }
}