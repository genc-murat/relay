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
    /// Unit tests for DecorrelatedJitterRetryStrategy thread-safety.
    /// </summary>
    public class DecorrelatedJitterRetryStrategyThreadSafetyTests
    {
        #region Thread-Safety Tests

        [Fact]
        public async Task Strategy_Should_BeThreadSafe_WithConcurrentCalls()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");
            var tasks = new List<Task>();

            // Act - Execute many concurrent calls from different threads
            for (int i = 0; i < 100; i++)
            {
                var attempt = i % 10 + 1; // Attempts 1-10
                tasks.Add(Task.Run(async () =>
                {
                    await strategy.ShouldRetryAsync(attempt, exception);
                    await strategy.GetRetryDelayAsync(attempt, exception);
                    return Task.CompletedTask;
                }));
            }

            // Assert - All tasks should complete without exceptions
            Func<Task> act = async () => await Task.WhenAll(tasks);
            await act();
        }

        [Fact]
        public async Task Strategy_Should_ProduceDifferentDelays_AcrossThreads()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");
            var delays = new System.Collections.Concurrent.ConcurrentBag<TimeSpan>();

            // Act - Get delays from multiple threads simultaneously
            var tasks = Enumerable.Range(0, 50).Select(async _ =>
            {
                var delay = await strategy.GetRetryDelayAsync(3, exception);
                delays.Add(delay);
                return Task.CompletedTask;
            }).ToArray();

            await Task.WhenAll(tasks);

            // Assert - Should have good variance due to different Random instances per thread
            var distinctDelays = delays.Distinct().Count();
            Assert.True(distinctDelays > 10, "different threads should produce varied delays");
        }

        #endregion
    }
}