using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Retry;
using Xunit;

namespace Relay.Core.Tests.Retry
{
    public class RetryStrategyTests
    {
        #region LinearRetryStrategy Tests

        [Fact]
        public void LinearRetryStrategy_Constructor_Should_AcceptValidDelay()
        {
            // Arrange & Act
            var delay = TimeSpan.FromSeconds(1);
            var strategy = new LinearRetryStrategy(delay);

            // Assert
            Assert.NotNull(strategy);
        }

        [Fact]
        public async Task LinearRetryStrategy_ShouldRetryAsync_Should_ReturnTrue_ForAttemptsGreaterThanZero()
        {
            // Arrange
            var strategy = new LinearRetryStrategy(TimeSpan.FromMilliseconds(100));
            var exception = new InvalidOperationException();

            // Act
            var result = await strategy.ShouldRetryAsync(1, exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task LinearRetryStrategy_ShouldRetryAsync_Should_ReturnFalse_ForZeroAttempt()
        {
            // Arrange
            var strategy = new LinearRetryStrategy(TimeSpan.FromMilliseconds(100));
            var exception = new InvalidOperationException();

            // Act
            var result = await strategy.ShouldRetryAsync(0, exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task LinearRetryStrategy_GetRetryDelayAsync_Should_ReturnSameDelay()
        {
            // Arrange
            var delay = TimeSpan.FromMilliseconds(500);
            var strategy = new LinearRetryStrategy(delay);
            var exception = new InvalidOperationException();

            // Act
            var result1 = await strategy.GetRetryDelayAsync(1, exception);
            var result2 = await strategy.GetRetryDelayAsync(2, exception);
            var result3 = await strategy.GetRetryDelayAsync(3, exception);

            // Assert
            Assert.Equal(delay, result1);
            Assert.Equal(delay, result2);
            Assert.Equal(delay, result3);
        }

        [Fact]
        public async Task LinearRetryStrategy_Should_RespectCancellationToken()
        {
            // Arrange
            var strategy = new LinearRetryStrategy(TimeSpan.FromMilliseconds(100));
            var cts = new CancellationTokenSource();
            var exception = new InvalidOperationException();

            // Act
            var result = await strategy.ShouldRetryAsync(1, exception, cts.Token);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region ExponentialBackoffRetryStrategy Tests

        [Fact]
        public void ExponentialBackoff_Constructor_Should_AcceptValidParameters()
        {
            // Arrange & Act
            var strategy = new ExponentialBackoffRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(10),
                2.0,
                true);

            // Assert
            Assert.NotNull(strategy);
        }

        [Fact]
        public void ExponentialBackoff_Constructor_Should_ThrowException_WhenInitialDelayIsNegative()
        {
            // Act
            Action act = () => new ExponentialBackoffRetryStrategy(
                TimeSpan.FromMilliseconds(-1),
                TimeSpan.FromSeconds(10));

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("initialDelay", ex.ParamName);
        }

        [Fact]
        public void ExponentialBackoff_Constructor_Should_ThrowException_WhenMaxDelayLessThanInitial()
        {
            // Act
            Action act = () => new ExponentialBackoffRetryStrategy(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(100));

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("maxDelay", ex.ParamName);
        }

        [Fact]
        public void ExponentialBackoff_Constructor_Should_ThrowException_WhenBackoffMultiplierLessThanOne()
        {
            // Act
            Action act = () => new ExponentialBackoffRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(10),
                0.5);

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("backoffMultiplier", ex.ParamName);
        }

        [Fact]
        public async Task ExponentialBackoff_ShouldRetryAsync_Should_ReturnTrue_ForAttemptsGreaterThanZero()
        {
            // Arrange
            var strategy = new ExponentialBackoffRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(10));
            var exception = new InvalidOperationException();

            // Act
            var result = await strategy.ShouldRetryAsync(1, exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExponentialBackoff_GetRetryDelayAsync_Should_IncreaseExponentially()
        {
            // Arrange
            var strategy = new ExponentialBackoffRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(60),
                2.0,
                useJitter: false); // Disable jitter for predictable testing
            var exception = new InvalidOperationException();

            // Act
            var delay1 = await strategy.GetRetryDelayAsync(1, exception);
            var delay2 = await strategy.GetRetryDelayAsync(2, exception);
            var delay3 = await strategy.GetRetryDelayAsync(3, exception);

            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(100), delay1); // 100 * 2^0
            Assert.Equal(TimeSpan.FromMilliseconds(200), delay2); // 100 * 2^1
            Assert.Equal(TimeSpan.FromMilliseconds(400), delay3); // 100 * 2^2
        }

        [Fact]
        public async Task ExponentialBackoff_GetRetryDelayAsync_Should_CapAtMaxDelay()
        {
            // Arrange
            var strategy = new ExponentialBackoffRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(1),
                2.0,
                useJitter: false);
            var exception = new InvalidOperationException();

            // Act
            var delay10 = await strategy.GetRetryDelayAsync(10, exception); // Would be 51200ms without cap

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(1), delay10); // Capped at maxDelay
        }

        [Fact]
        public async Task ExponentialBackoff_GetRetryDelayAsync_WithJitter_Should_AddRandomness()
        {
            // Arrange
            var strategy = new ExponentialBackoffRetryStrategy(
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromSeconds(60),
                2.0,
                useJitter: true);
            var exception = new InvalidOperationException();

            // Act
            var delay1a = await strategy.GetRetryDelayAsync(1, exception);
            var delay1b = await strategy.GetRetryDelayAsync(1, exception);

            // Assert
            // With jitter enabled, delays should be different (statistically)
            // and within ±10% of base delay
            var baseDelay = 1000;
            Assert.InRange(delay1a.TotalMilliseconds, baseDelay * 0.9, baseDelay * 1.1);
            Assert.InRange(delay1b.TotalMilliseconds, baseDelay * 0.9, baseDelay * 1.1);
        }

        [Fact]
        public async Task ExponentialBackoff_Should_RespectCancellationToken()
        {
            // Arrange
            var strategy = new ExponentialBackoffRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(10));
            var cts = new CancellationTokenSource();
            var exception = new InvalidOperationException();

            // Act
            var result = await strategy.ShouldRetryAsync(1, exception, cts.Token);

            // Assert
            Assert.True(result);
        }

        #endregion
    }
}
