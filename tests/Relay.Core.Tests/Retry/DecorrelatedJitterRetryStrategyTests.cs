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
    /// Comprehensive unit tests for the DecorrelatedJitterRetryStrategy.
    /// Tests cover validation, retry logic, delay calculations, thread-safety, and edge cases.
    /// </summary>
    public class DecorrelatedJitterRetryStrategyTests
    {
        #region Constructor Validation Tests

        [Fact]
        public void Constructor_Should_AcceptValidParameters()
        {
            // Arrange & Act
            var strategy = new DecorrelatedJitterRetryStrategy(
                baseDelay: TimeSpan.FromMilliseconds(100),
                maxDelay: TimeSpan.FromSeconds(30),
                maxAttempts: 5);
 
            // Assert
            Assert.NotNull(strategy);
        }

        [Fact]
        public void Constructor_Should_AcceptNullMaxAttempts()
        {
            // Arrange & Act
            var strategy = new DecorrelatedJitterRetryStrategy(
                baseDelay: TimeSpan.FromMilliseconds(100),
                maxDelay: TimeSpan.FromSeconds(30),
                maxAttempts: null);
 
            // Assert
            Assert.NotNull(strategy);
        }

        [Fact]
        public void Constructor_Should_AcceptEqualBaseAndMaxDelay()
        {
            // Arrange & Act
            var delay = TimeSpan.FromSeconds(5);
            var strategy = new DecorrelatedJitterRetryStrategy(
                baseDelay: delay,
                maxDelay: delay);
 
            // Assert
            Assert.NotNull(strategy);
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenBaseDelayIsNegative()
        {
            // Act
            Action act = () => new DecorrelatedJitterRetryStrategy(
                baseDelay: TimeSpan.FromMilliseconds(-1),
                maxDelay: TimeSpan.FromSeconds(30));

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("baseDelay", ex.ParamName);
            Assert.Contains("must be non-negative", ex.Message);
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenMaxDelayIsLessThanBaseDelay()
        {
            // Act
            Action act = () => new DecorrelatedJitterRetryStrategy(
                baseDelay: TimeSpan.FromSeconds(30),
                maxDelay: TimeSpan.FromMilliseconds(100));

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("maxDelay", ex.ParamName);
            Assert.Contains("must be greater than or equal to base delay", ex.Message);
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenMaxAttemptsIsZero()
        {
            // Act
            Action act = () => new DecorrelatedJitterRetryStrategy(
                baseDelay: TimeSpan.FromMilliseconds(100),
                maxDelay: TimeSpan.FromSeconds(30),
                maxAttempts: 0);
 
            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("maxAttempts", ex.ParamName);
            Assert.Contains("must be at least 1", ex.Message);
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenMaxAttemptsIsNegative()
        {
            // Act
            Action act = () => new DecorrelatedJitterRetryStrategy(
                baseDelay: TimeSpan.FromMilliseconds(100),
                maxDelay: TimeSpan.FromSeconds(30),
                maxAttempts: -1);
 
            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("maxAttempts", ex.ParamName);
            Assert.Contains("must be at least 1", ex.Message);
        }

        #endregion

        #region ShouldRetryAsync Tests

        [Fact]
        public async Task ShouldRetryAsync_Should_ReturnFalse_ForAttemptZero()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = await strategy.ShouldRetryAsync(0, exception);

            // Assert
            Assert.False(result, "attempt 0 is the initial execution, not a retry");
        }

        [Fact]
        public async Task ShouldRetryAsync_Should_ReturnFalse_ForNegativeAttempt()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = await strategy.ShouldRetryAsync(-1, exception);

            // Assert
            Assert.False(result, "negative attempts are invalid");
        }

        [Fact]
        public async Task ShouldRetryAsync_Should_ReturnTrue_ForValidAttempts()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result1 = await strategy.ShouldRetryAsync(1, exception);
            var result2 = await strategy.ShouldRetryAsync(2, exception);
            var result5 = await strategy.ShouldRetryAsync(5, exception);

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result5);
        }

        [Fact]
        public async Task ShouldRetryAsync_Should_ReturnTrue_WhenMaxAttemptsIsNull()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30),
                maxAttempts: null);
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result1 = await strategy.ShouldRetryAsync(1, exception);
            var result100 = await strategy.ShouldRetryAsync(100, exception);

            // Assert
            Assert.True(result1);
            Assert.True(result100, "no max attempts limit should allow unlimited retries");
        }

        [Fact]
        public async Task ShouldRetryAsync_Should_RespectMaxAttempts()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30),
                maxAttempts: 3);
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result1 = await strategy.ShouldRetryAsync(1, exception);
            var result2 = await strategy.ShouldRetryAsync(2, exception);
            var result3 = await strategy.ShouldRetryAsync(3, exception);
            var result4 = await strategy.ShouldRetryAsync(4, exception);

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.False(result4, "attempt 4 exceeds maxAttempts of 3");
        }

        [Fact]
        public async Task ShouldRetryAsync_Should_RespectCancellationToken()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var cts = new CancellationTokenSource();
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = await strategy.ShouldRetryAsync(1, exception, cts.Token);

            // Assert
            Assert.True(result);
            Assert.False(cts.Token.IsCancellationRequested);
        }

        #endregion

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
                    var shouldRetry = await strategy.ShouldRetryAsync(attempt, exception);
                    var delay = await strategy.GetRetryDelayAsync(attempt, exception);

                    // Basic validation
                    Assert.True(shouldRetry);
                    Assert.True(delay >= TimeSpan.Zero);
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
            var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(async () =>
            {
                var delay = await strategy.GetRetryDelayAsync(3, exception);
                delays.Add(delay);
            }));

            await Task.WhenAll(tasks);

            // Assert - Should have good variance due to different Random instances per thread
            var distinctDelays = delays.Distinct().Count();
            Assert.True(distinctDelays > 10, "different threads should produce varied delays");
        }

        #endregion

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
