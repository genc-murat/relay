using Moq;
using Relay.Core.Retry.Strategies;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Retry.Strategies
{
    /// <summary>
    /// Edge case and state transition tests for CircuitBreakerRetryStrategy.
    /// </summary>
    public class CircuitBreakerRetryStrategyEdgeCaseTests
    {
        #region Boundary Value Tests

        [Fact]
        public async Task ShouldRetryAsync_WithExactlyThresholdFailures_ShouldOpenCircuit()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 5);
            var exception = new InvalidOperationException("Test");

            // Act - Reach exactly the threshold
            for (int i = 0; i < 5; i++)
            {
                await strategy.ShouldRetryAsync(i + 1, exception);
            }

            // Try one more time - should fail
            var result = await strategy.ShouldRetryAsync(6, exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldRetryAsync_WithOneMoreThanThreshold_ShouldRemainOpen()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 5);
            var exception = new InvalidOperationException("Test");

            // Act - Exceed threshold multiple times
            for (int i = 0; i < 5; i++)
            {
                await strategy.ShouldRetryAsync(i + 1, exception);
            }

            var result1 = await strategy.ShouldRetryAsync(6, exception);
            var result2 = await strategy.ShouldRetryAsync(7, exception);
            var result3 = await strategy.ShouldRetryAsync(8, exception);

            // Assert - Circuit remains open
            Assert.False(result1);
            Assert.False(result2);
            Assert.False(result3);
        }

        #endregion

        #region Rapid Failure Tests

        [Fact]
        public async Task ShouldRetryAsync_RapidConsecutiveFailures_ShouldOpenCircuitQuickly()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 2);
            var exception = new InvalidOperationException("Rapid failures");

            // Act - Rapid failures
            var results = new bool[5];
            for (int i = 0; i < 5; i++)
            {
                results[i] = await strategy.ShouldRetryAsync(i + 1, exception);
            }

            // Assert
            Assert.True(results[0]); // fail 1
            Assert.True(results[1]); // fail 2
            Assert.False(results[2]); // circuit opens
            Assert.False(results[3]); // stays open
            Assert.False(results[4]); // stays open
        }

        #endregion

        #region Mixed Success and Failure Tests

        [Fact]
        public async Task ShouldRetryAsync_AlternatingSuccessAndFailure_ShouldResetCounterEachSuccess()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.SetupSequence(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)   // fail
                .ReturnsAsync(false)  // success - reset
                .ReturnsAsync(true)   // fail
                .ReturnsAsync(true)   // fail
                .ReturnsAsync(false)  // success - reset
                .ReturnsAsync(true);  // fail allowed

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
            var exception = new InvalidOperationException("Test");

            // Act
            var results = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                results[i] = await strategy.ShouldRetryAsync(i + 1, exception);
            }

            // Assert
            Assert.True(results[0]);
            Assert.False(results[1]);
            Assert.True(results[2]);
            Assert.True(results[3]);
            Assert.False(results[4]);
            Assert.True(results[5]);
        }

        [Fact]
        public async Task ShouldRetryAsync_SuccessResetsFailureCount_ShouldAllowCircuitReopening()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.SetupSequence(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)   // call 1: fail 1, consecutiveFailures = 1
                .ReturnsAsync(true)   // call 2: fail 2, consecutiveFailures = 2
                .ReturnsAsync(true)   // call 3: fail 3, consecutiveFailures = 3, circuit opens
                // calls 4-5 don't call inner strategy (circuit open)
                .ReturnsAsync(false)  // call 4: success - reset counter, consecutiveFailures = 0
                .ReturnsAsync(true)   // call 5: fail 1 again, consecutiveFailures = 1
                .ReturnsAsync(true);  // call 6: fail 2 again, consecutiveFailures = 2

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
            var exception = new InvalidOperationException("Test");

            // Act
            var result1 = await strategy.ShouldRetryAsync(1, exception);  // call inner (1), returns true, consecutiveFailures = 1
            var result2 = await strategy.ShouldRetryAsync(2, exception);  // call inner (2), returns true, consecutiveFailures = 2
            var result3 = await strategy.ShouldRetryAsync(3, exception);  // call inner (3), returns true, consecutiveFailures = 3, circuit opens
            var result4 = await strategy.ShouldRetryAsync(4, exception);  // circuit check: 3 >= 3, return false (don't call inner)

            strategy.Reset();

            var result5 = await strategy.ShouldRetryAsync(5, exception);  // call inner (4), returns false, consecutiveFailures = 0
            var result6 = await strategy.ShouldRetryAsync(6, exception);  // call inner (5), returns true, consecutiveFailures = 1
            var result7 = await strategy.ShouldRetryAsync(7, exception);  // call inner (6), returns true, consecutiveFailures = 2

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.False(result4);  // Circuit open
            Assert.False(result5);  // Success - returns false
            Assert.True(result6);   // After reset
            Assert.True(result7);
        }

        #endregion

        #region Inner Strategy Behavior Tests

        [Fact]
        public async Task ShouldRetryAsync_WhenInnerStrategyThrowsException_ShouldPropagate()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Inner strategy error"));

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);
            var exception = new ArgumentException("Original error");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await strategy.ShouldRetryAsync(1, exception));
        }

        [Fact]
        public async Task GetRetryDelayAsync_WhenInnerStrategyReturnsZeroDelay_ShouldReturnZero()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.GetRetryDelayAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TimeSpan.Zero);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);
            var exception = new InvalidOperationException("Test");

            // Act
            var delay = await strategy.GetRetryDelayAsync(1, exception);

            // Assert
            Assert.Equal(TimeSpan.Zero, delay);
        }

        [Fact]
        public async Task GetRetryDelayAsync_WhenInnerStrategyReturnsVeryLargeDelay_ShouldReturnUnchanged()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            var expectedDelay = TimeSpan.FromHours(1);
            mockInnerStrategy.Setup(s => s.GetRetryDelayAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDelay);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);
            var exception = new InvalidOperationException("Test");

            // Act
            var delay = await strategy.GetRetryDelayAsync(1, exception);

            // Assert
            Assert.Equal(expectedDelay, delay);
        }

        #endregion

        #region State Persistence Tests

        [Fact]
        public async Task MultipleStrategies_ShouldMaintainIndependentState()
        {
            // Arrange
            var mockInnerStrategy1 = new Mock<IRetryStrategy>();
            mockInnerStrategy1.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var mockInnerStrategy2 = new Mock<IRetryStrategy>();
            mockInnerStrategy2.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy1 = new CircuitBreakerRetryStrategy(mockInnerStrategy1.Object, failureThreshold: 2);
            var strategy2 = new CircuitBreakerRetryStrategy(mockInnerStrategy2.Object, failureThreshold: 5);
            var exception = new InvalidOperationException("Test");

            // Act - Open circuit in strategy1
            await strategy1.ShouldRetryAsync(1, exception);
            await strategy1.ShouldRetryAsync(2, exception);

            // strategy2 should still allow retries
            var strategy1Result = await strategy1.ShouldRetryAsync(3, exception);
            var strategy2Result = await strategy2.ShouldRetryAsync(1, exception);

            // Assert
            Assert.False(strategy1Result); // circuit open
            Assert.True(strategy2Result);  // still accepting
        }

        #endregion

        #region Concurrent Access Tests

        [Fact]
        public async Task ShouldRetryAsync_ConcurrentCalls_ShouldHandleRaceConditions()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 5);
            var exception = new InvalidOperationException("Test");

            // Act - Concurrent calls
            var tasks = new Task<bool>[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = strategy.ShouldRetryAsync(i + 1, exception).AsTask();
            }

            var results = await Task.WhenAll(tasks);

            // Assert - First 5 should succeed, rest should fail due to circuit
            int successCount = 0;
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i])
                    successCount++;
            }

            // Due to race conditions, we can't guarantee exact count, but circuit should open
            Assert.True(successCount <= 5, "Circuit should have opened after threshold");
        }

        #endregion

        #region Reset Functionality Tests

        [Fact]
        public async Task Reset_MultipleResets_ShouldReopenCircuitEachTime()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 2);
            var exception = new InvalidOperationException("Test");

            // Act - Open circuit
            await strategy.ShouldRetryAsync(1, exception);
            await strategy.ShouldRetryAsync(2, exception);
            var openResult1 = await strategy.ShouldRetryAsync(3, exception);

            // Reset and reopen
            strategy.Reset();
            await strategy.ShouldRetryAsync(4, exception);
            await strategy.ShouldRetryAsync(5, exception);
            var openResult2 = await strategy.ShouldRetryAsync(6, exception);

            // Reset again
            strategy.Reset();
            var closedResult = await strategy.ShouldRetryAsync(7, exception);

            // Assert
            Assert.False(openResult1);
            Assert.False(openResult2);
            Assert.True(closedResult);
        }

        [Fact]
        public async Task Reset_BetweenAttempts_ShouldClearCircuitState()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
            var exception = new InvalidOperationException("Test");

            // Act - Some failures
            await strategy.ShouldRetryAsync(1, exception);
            strategy.Reset();

            // Now try again with fresh state
            var result1 = await strategy.ShouldRetryAsync(2, exception);
            var result2 = await strategy.ShouldRetryAsync(3, exception);
            var result3 = await strategy.ShouldRetryAsync(4, exception);

            // Assert - Should allow 3 more retries
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
        }

        #endregion

        #region Threshold Value Tests

        [Fact]
        public async Task WithVeryHighThreshold_ShouldRequireManyFailuresToOpen()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 100);
            var exception = new InvalidOperationException("Test");

            // Act - Try 99 times (consecutiveFailures will be 1-99)
            bool allSuccess = true;
            for (int i = 0; i < 99; i++)
            {
                var result = await strategy.ShouldRetryAsync(i + 1, exception);
                if (!result)
                {
                    allSuccess = false;
                    break;
                }
            }

            // On the 100th attempt, _consecutiveFailures == 99, after inner returns true, it becomes 100
            // On the 101st attempt, _consecutiveFailures >= 100, circuit opens
            var result100 = await strategy.ShouldRetryAsync(100, exception);
            var result101 = await strategy.ShouldRetryAsync(101, exception);

            // Assert
            Assert.True(allSuccess);
            Assert.True(result100);  // Still allows 100th attempt (consecutiveFailures becomes 100)
            Assert.False(result101); // 101st attempt opens circuit
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task ShouldRetryAsync_WithNullException_ShouldPassToInnerStrategy()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);

            // Act
            var result = await strategy.ShouldRetryAsync(1, null);

            // Assert
            Assert.True(result);
            mockInnerStrategy.Verify(s => s.ShouldRetryAsync(1, null, default), Times.Once);
        }

        [Fact]
        public async Task ShouldRetryAsync_WithDifferentExceptionEachTime_ShouldTrackCumulativeFailures()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);

            // Act - Different exceptions
            var result1 = await strategy.ShouldRetryAsync(1, new InvalidOperationException());
            var result2 = await strategy.ShouldRetryAsync(2, new ArgumentException());
            var result3 = await strategy.ShouldRetryAsync(3, new TimeoutException());
            var result4 = await strategy.ShouldRetryAsync(4, new NotSupportedException());

            // Assert - Circuit should open regardless of exception type
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.False(result4);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteLifecycle_OpenCloseReopen_ShouldWork()
        {
            // Arrange
            var mockInnerStrategy = new Mock<IRetryStrategy>();
            mockInnerStrategy.SetupSequence(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)   // fail 1 - consecutiveFailures = 1
                .ReturnsAsync(true)   // fail 2 - consecutiveFailures = 2, circuit opens on next call
                .ReturnsAsync(true)   // fail 1 after reset
                .ReturnsAsync(true);  // fail 2 after reset

            var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 2);
            var exception = new InvalidOperationException("Test");

            // Act
            var step1 = await strategy.ShouldRetryAsync(1, exception);  // consecutiveFailures = 1
            var step2 = await strategy.ShouldRetryAsync(2, exception);  // consecutiveFailures = 2
            var step3 = await strategy.ShouldRetryAsync(3, exception);  // Circuit open - returns false immediately

            strategy.Reset();

            var step4 = await strategy.ShouldRetryAsync(4, exception);  // consecutiveFailures = 1 after reset
            var step5 = await strategy.ShouldRetryAsync(5, exception);  // consecutiveFailures = 2
            var step6 = await strategy.ShouldRetryAsync(6, exception);  // Circuit open again

            // Assert
            Assert.True(step1);
            Assert.True(step2);
            Assert.False(step3);  // Circuit open
            Assert.True(step4);   // After reset
            Assert.True(step5);
            Assert.False(step6);  // Circuit open again
        }

        #endregion
    }
}
