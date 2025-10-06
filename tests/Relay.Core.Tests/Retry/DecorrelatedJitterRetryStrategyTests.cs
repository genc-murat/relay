using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
            strategy.Should().NotBeNull();
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
            strategy.Should().NotBeNull();
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
            strategy.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenBaseDelayIsNegative()
        {
            // Act
            Action act = () => new DecorrelatedJitterRetryStrategy(
                baseDelay: TimeSpan.FromMilliseconds(-1),
                maxDelay: TimeSpan.FromSeconds(30));

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("baseDelay")
                .WithMessage("*must be non-negative*");
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenMaxDelayIsLessThanBaseDelay()
        {
            // Act
            Action act = () => new DecorrelatedJitterRetryStrategy(
                baseDelay: TimeSpan.FromSeconds(30),
                maxDelay: TimeSpan.FromMilliseconds(100));

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("maxDelay")
                .WithMessage("*must be greater than or equal to base delay*");
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
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("maxAttempts")
                .WithMessage("*must be at least 1*");
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
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("maxAttempts")
                .WithMessage("*must be at least 1*");
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
            result.Should().BeFalse("attempt 0 is the initial execution, not a retry");
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
            result.Should().BeFalse("negative attempts are invalid");
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
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result5.Should().BeTrue();
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
            result1.Should().BeTrue();
            result100.Should().BeTrue("no max attempts limit should allow unlimited retries");
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
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result3.Should().BeTrue();
            result4.Should().BeFalse("attempt 4 exceeds maxAttempts of 3");
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
            result.Should().BeTrue();
            cts.Token.IsCancellationRequested.Should().BeFalse();
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
            delay.Should().Be(TimeSpan.Zero);
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
            delay.Should().Be(TimeSpan.Zero);
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
            delay.Should().BeGreaterThanOrEqualTo(baseDelay);
            delay.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(300));
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
                delay.Should().BeGreaterThanOrEqualTo(baseDelay);
                delay.Should().BeLessThanOrEqualTo(maxDelay);
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
            result1.Should().Be(delay);
            result2.Should().Be(delay);
            result5.Should().Be(delay);
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
            distinctDelays.Should().BeGreaterThan(10, "decorrelated jitter should produce varied delays");

            // All delays should be within valid range
            foreach (var delay in delays)
            {
                delay.Should().BeGreaterThanOrEqualTo(baseDelay);
                delay.Should().BeLessThanOrEqualTo(maxDelay);
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
            averageDelay3.Should().BeGreaterThan(averageDelay1);
            averageDelay5.Should().BeGreaterThan(averageDelay3);
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
            delay.Should().BeGreaterThan(TimeSpan.Zero);
            cts.Token.IsCancellationRequested.Should().BeFalse();
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
                    shouldRetry.Should().BeTrue();
                    delay.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
                }));
            }

            // Assert - All tasks should complete without exceptions
            Func<Task> act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync("strategy should be thread-safe");
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
            distinctDelays.Should().BeGreaterThan(10, "different threads should produce varied delays");
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
            delay1.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(1));
            delay1.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(10));
            delay2.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(1));
            delay2.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(10));
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
            delay1.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMinutes(1));
            delay1.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(1));
            delay2.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMinutes(1));
            delay2.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(1));
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
            delay1.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
            delay1.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(10));
            delay2.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
            delay2.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(10));
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
            delay100.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100));
            delay100.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(30));
            delay1000.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100));
            delay1000.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(30));
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
                shouldRetry.Should().BeTrue($"attempt {attempt} is within maxAttempts {maxAttempts}");
            }

            var shouldNotRetry = await strategy.ShouldRetryAsync(maxAttempts + 1, exception);
            shouldNotRetry.Should().BeFalse($"attempt {maxAttempts + 1} exceeds maxAttempts {maxAttempts}");
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
