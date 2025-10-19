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
    /// Unit tests for DecorrelatedJitterRetryStrategy disposal functionality.
    /// </summary>
    public class DecorrelatedJitterRetryStrategyDisposalTests
    {
        #region Disposal Tests

        [Fact]
        public void Dispose_Should_CleanUpResources()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));

            // Act
            strategy.Dispose();

            // Assert - Should not throw on multiple dispose calls
            strategy.Dispose();
        }

        [Fact]
        public async Task ShouldRetryAsync_Should_ThrowAfterDispose()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            strategy.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => strategy.ShouldRetryAsync(1, exception).AsTask());
        }

        [Fact]
        public async Task GetRetryDelayAsync_Should_ThrowAfterDispose()
        {
            // Arrange
            var strategy = new DecorrelatedJitterRetryStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30));
            var exception = new InvalidOperationException("Test exception");

            strategy.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => strategy.GetRetryDelayAsync(1, exception).AsTask());
        }

        #endregion
    }
}