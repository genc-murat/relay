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
    /// Additional edge case tests for DecorrelatedJitterRetryStrategy.
    /// </summary>
    public class DecorrelatedJitterRetryStrategyEdgeCasesTests
    {
        #region Additional Edge Cases

        [Fact]
        public async Task GetRetryDelayAsync_Should_HandleVeryHighAttemptNumbers()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay = await strategy.GetRetryDelayAsync(int.MaxValue, exception);

            // Assert - Should still respect bounds even for extreme attempt numbers
            Assert.InRange(delay, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task Strategy_Should_HandleNullException()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));

            // Act
            var shouldRetry = await strategy.ShouldRetryAsync(1, null!);
            var delay = await strategy.GetRetryDelayAsync(1, null!);

            // Assert
            Assert.True(shouldRetry);
            Assert.True(delay >= TimeSpan.Zero);
        }

        [Fact]
        public async Task Strategy_Should_WorkWithCancelledCancellationToken()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var exception = new InvalidOperationException("Test exception");

            // Act & Assert - Should still work even with cancelled token (no async delay)
            var shouldRetry = await strategy.ShouldRetryAsync(1, exception, cts.Token);
            var delay = await strategy.GetRetryDelayAsync(1, exception, cts.Token);

            Assert.True(shouldRetry);
            Assert.True(delay >= TimeSpan.Zero);
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_ProduceConsistentSequence_ForSameStrategyInstance()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            // Act - Get delays for consecutive attempts
            var delay1 = await strategy.GetRetryDelayAsync(1, exception);
            var delay2 = await strategy.GetRetryDelayAsync(2, exception);
            var delay3 = await strategy.GetRetryDelayAsync(3, exception);

            // Assert - Each delay should be within valid range
            Assert.InRange(delay1, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30));
            Assert.InRange(delay2, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30));
            Assert.InRange(delay3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task Strategy_Should_HandleVerySmallTimeSpans()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromTicks(1),
                TimeSpan.FromTicks(10));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay = await strategy.GetRetryDelayAsync(1, exception);

            // Assert
            Assert.InRange(delay, TimeSpan.FromTicks(1), TimeSpan.FromTicks(10));
        }

        [Fact]
        public async Task Strategy_Should_HandleVeryLargeTimeSpans()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromDays(1),
                TimeSpan.FromDays(365));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay = await strategy.GetRetryDelayAsync(1, exception);

            // Assert
            Assert.InRange(delay, TimeSpan.FromDays(1), TimeSpan.FromDays(365));
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_HandleEdgeCase_WhereRangeCalculationResultsInZero()
        {
            // Arrange - Create scenario where baseDelay * 3 > maxDelay, and baseDelay == maxDelay
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(10)); // baseDelay == maxDelay
            var exception = new InvalidOperationException("Test exception");

            // Act
            var delay = await strategy.GetRetryDelayAsync(1, exception);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(10), delay);
        }

        [Fact]
        public async Task Strategy_Should_WorkWithDifferentExceptionTypes()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));

            var exceptions = new Exception[]
            {
                new InvalidOperationException("Test"),
                new ArgumentException("Test"),
                new TimeoutException("Test"),
                new System.IO.IOException("Test"),
                new CustomException("Test")
            };

            // Act & Assert - Strategy should work regardless of exception type
            foreach (var ex in exceptions)
            {
                var shouldRetry = await strategy.ShouldRetryAsync(1, ex);
                var delay = await strategy.GetRetryDelayAsync(1, ex);

                Assert.True(shouldRetry);
                Assert.True(delay >= TimeSpan.Zero);
            }
        }

        [Fact]
        public async Task Strategy_Should_BeReusable_AcrossMultipleOperations()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            // Act - Simulate multiple independent operations
            for (int operation = 1; operation <= 5; operation++)
            {
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    var shouldRetry = await strategy.ShouldRetryAsync(attempt, exception);
                    var delay = await strategy.GetRetryDelayAsync(attempt, exception);

                    Assert.True(shouldRetry);
                    Assert.InRange(delay, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30));
                }
            }
        }

        #endregion

        #region Helper Classes

        private class CustomException : Exception
        {
            public CustomException(string message) : base(message) { }
        }

        #endregion
    }
}