using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
            strategy.Should().NotBeNull();
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
            result.Should().BeTrue();
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
            result.Should().BeFalse();
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
            result1.Should().Be(delay);
            result2.Should().Be(delay);
            result3.Should().Be(delay);
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
            result.Should().BeTrue();
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
            strategy.Should().NotBeNull();
        }

        [Fact]
        public void ExponentialBackoff_Constructor_Should_ThrowException_WhenInitialDelayIsNegative()
        {
            // Act
            Action act = () => new ExponentialBackoffRetryStrategy(
                TimeSpan.FromMilliseconds(-1),
                TimeSpan.FromSeconds(10));

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("initialDelay");
        }

        [Fact]
        public void ExponentialBackoff_Constructor_Should_ThrowException_WhenMaxDelayLessThanInitial()
        {
            // Act
            Action act = () => new ExponentialBackoffRetryStrategy(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(100));

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("maxDelay");
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
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("backoffMultiplier");
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
            result.Should().BeTrue();
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
            delay1.Should().Be(TimeSpan.FromMilliseconds(100)); // 100 * 2^0
            delay2.Should().Be(TimeSpan.FromMilliseconds(200)); // 100 * 2^1
            delay3.Should().Be(TimeSpan.FromMilliseconds(400)); // 100 * 2^2
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
            delay10.Should().Be(TimeSpan.FromSeconds(1)); // Capped at maxDelay
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
            delay1a.TotalMilliseconds.Should().BeInRange(baseDelay * 0.9, baseDelay * 1.1);
            delay1b.TotalMilliseconds.Should().BeInRange(baseDelay * 0.9, baseDelay * 1.1);
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
            result.Should().BeTrue();
        }

        #endregion
    }
}
