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
    /// Unit tests for DecorrelatedJitterRetryStrategy constructor validation.
    /// </summary>
    public class DecorrelatedJitterRetryStrategyConstructorTests
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
    }
}