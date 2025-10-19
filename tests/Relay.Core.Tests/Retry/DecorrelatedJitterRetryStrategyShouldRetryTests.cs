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
    /// Unit tests for DecorrelatedJitterRetryStrategy ShouldRetryAsync functionality.
    /// </summary>
    public class DecorrelatedJitterRetryStrategyShouldRetryTests
    {
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
    }
}