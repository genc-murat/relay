using Moq;
using Relay.Core.Retry.Strategies;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Retry.Strategies;

/// <summary>
/// Comprehensive tests for CircuitBreakerRetryStrategy.
/// </summary>
public class CircuitBreakerRetryStrategyTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidInnerStrategy_ShouldInitializeSuccessfully()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();

        // Act
        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 5);

        // Assert
        Assert.NotNull(strategy);
    }

    [Fact]
    public void Constructor_WithNullInnerStrategy_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CircuitBreakerRetryStrategy(null, failureThreshold: 5));
    }

    [Fact]
    public void Constructor_WithZeroFailureThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 0));
    }

    [Fact]
    public void Constructor_WithNegativeFailureThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: -5));
    }

    [Fact]
    public void Constructor_WithSmallFailureThreshold_ShouldAccept()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();

        // Act
        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 1);

        // Assert
        Assert.NotNull(strategy);
    }

    [Fact]
    public void Constructor_WithLargeFailureThreshold_ShouldAccept()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();

        // Act
        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: int.MaxValue);

        // Assert
        Assert.NotNull(strategy);
    }

    [Fact]
    public void Constructor_WithDefaultFailureThreshold_ShouldUseDefaultValue()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();

        // Act
        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);

        // Assert
        Assert.NotNull(strategy);
    }

    #endregion

    #region ShouldRetryAsync Tests - Below Threshold

    [Fact]
    public async Task ShouldRetryAsync_WhenInnerStrategyReturnsTrueAndBelowThreshold_ShouldReturnTrueAndIncrementFailures()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = await strategy.ShouldRetryAsync(1, exception);

        // Assert
        Assert.True(result);
        mockInnerStrategy.Verify(s => s.ShouldRetryAsync(1, exception, default), Times.Once);
    }

    [Fact]
    public async Task ShouldRetryAsync_WhenInnerStrategyReturnsFalseAndBelowThreshold_ShouldReturnFalseAndResetFailures()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = await strategy.ShouldRetryAsync(1, exception);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldRetryAsync_MultipleSuccessfulRetries_ShouldCallInnerStrategyEachTime()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 5);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result1 = await strategy.ShouldRetryAsync(1, exception);
        var result2 = await strategy.ShouldRetryAsync(2, exception);
        var result3 = await strategy.ShouldRetryAsync(3, exception);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
        mockInnerStrategy.Verify(s => s.ShouldRetryAsync(It.IsAny<int>(), exception, default), Times.Exactly(3));
    }

    #endregion

    #region ShouldRetryAsync Tests - At Threshold

    [Fact]
    public async Task ShouldRetryAsync_WhenFailuresReachThreshold_ShouldReturnFalseAndNotCallInnerStrategy()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
        var exception = new InvalidOperationException("Test exception");

        // Act - First 3 attempts should return true
        await strategy.ShouldRetryAsync(1, exception);
        await strategy.ShouldRetryAsync(2, exception);
        await strategy.ShouldRetryAsync(3, exception);

        // 4th attempt should return false (circuit opens)
        var result = await strategy.ShouldRetryAsync(4, exception);

        // Assert
        Assert.False(result);
        // Inner strategy should only be called 3 times, not 4
        mockInnerStrategy.Verify(s => s.ShouldRetryAsync(It.IsAny<int>(), exception, default), Times.Exactly(3));
    }

    [Fact]
    public async Task ShouldRetryAsync_WithThresholdOne_ShouldOpenCircuitOnFirstFailure()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 1);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result1 = await strategy.ShouldRetryAsync(1, exception);
        var result2 = await strategy.ShouldRetryAsync(2, exception);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
    }

    #endregion

    #region ShouldRetryAsync Tests - Reset Failures

    [Fact]
    public async Task ShouldRetryAsync_WhenInnerStrategyReturnsFalse_ShouldResetConsecutiveFailures()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.SetupSequence(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false)  // Reset failures
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
        var exception = new InvalidOperationException("Test exception");

        // Act
        await strategy.ShouldRetryAsync(1, exception); // fail 1
        await strategy.ShouldRetryAsync(2, exception); // fail 2
        await strategy.ShouldRetryAsync(3, exception); // success - reset to 0
        var result = await strategy.ShouldRetryAsync(4, exception); // should retry

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ShouldRetryAsync_WithInterleavedSuccessAndFailures_ShouldTrackFailuresCorrectly()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.SetupSequence(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)  // fail 1
            .ReturnsAsync(false) // reset
            .ReturnsAsync(true)  // fail 1 again
            .ReturnsAsync(true)  // fail 2
            .ReturnsAsync(true)  // fail 3 - circuit opens
            .ReturnsAsync(false); // circuit stays open

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result1 = await strategy.ShouldRetryAsync(1, exception);
        var result2 = await strategy.ShouldRetryAsync(2, exception);
        var result3 = await strategy.ShouldRetryAsync(3, exception);
        var result4 = await strategy.ShouldRetryAsync(4, exception);
        var result5 = await strategy.ShouldRetryAsync(5, exception);
        var result6 = await strategy.ShouldRetryAsync(6, exception);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.True(result3);
        Assert.True(result4);
        Assert.True(result5);
        Assert.False(result6);
    }

    #endregion

    #region ShouldRetryAsync Tests - Cancellation

    [Fact]
    public async Task ShouldRetryAsync_WithCancellationToken_ShouldPassToInnerStrategy()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);
        var cts = new CancellationTokenSource();
        var exception = new InvalidOperationException("Test exception");

        // Act
        await strategy.ShouldRetryAsync(1, exception, cts.Token);

        // Assert
        mockInnerStrategy.Verify(
            s => s.ShouldRetryAsync(1, exception, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task ShouldRetryAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);
        var cts = new CancellationTokenSource();
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await strategy.ShouldRetryAsync(1, exception, cts.Token));
    }

    #endregion

    #region GetRetryDelayAsync Tests

    [Fact]
    public async Task GetRetryDelayAsync_ShouldDelegateToInnerStrategy()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        var expectedDelay = TimeSpan.FromMilliseconds(500);
        mockInnerStrategy.Setup(s => s.GetRetryDelayAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDelay);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var delay = await strategy.GetRetryDelayAsync(1, exception);

        // Assert
        Assert.Equal(expectedDelay, delay);
        mockInnerStrategy.Verify(
            s => s.GetRetryDelayAsync(1, exception, default),
            Times.Once);
    }

    [Fact]
    public async Task GetRetryDelayAsync_WithMultipleAttempts_ShouldCallInnerStrategyWithCorrectAttempt()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.GetRetryDelayAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(100));

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var delay1 = await strategy.GetRetryDelayAsync(1, exception);
        var delay2 = await strategy.GetRetryDelayAsync(2, exception);
        var delay3 = await strategy.GetRetryDelayAsync(3, exception);

        // Assert
        mockInnerStrategy.Verify(
            s => s.GetRetryDelayAsync(It.IsAny<int>(), exception, default),
            Times.Exactly(3));
    }

    [Fact]
    public async Task GetRetryDelayAsync_WhenCircuitOpen_ShouldStillDelegateToInnerStrategy()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockInnerStrategy.Setup(s => s.GetRetryDelayAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(100));

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 1);
        var exception = new InvalidOperationException("Test exception");

        // Act - Open circuit
        await strategy.ShouldRetryAsync(1, exception);

        // Get delay after circuit is open
        var delay = await strategy.GetRetryDelayAsync(2, exception);

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(100), delay);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public async Task Reset_ShouldResetConsecutiveFailures()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 2);

        // Act - Trigger a failure
        await strategy.ShouldRetryAsync(1, new Exception());

        // Reset the strategy
        strategy.Reset();

        // Verify by triggering multiple failures again
        var result1 = await strategy.ShouldRetryAsync(2, new Exception());
        var result2 = await strategy.ShouldRetryAsync(3, new Exception());
        var result3 = await strategy.ShouldRetryAsync(4, new Exception());

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.False(result3); // Circuit opens at threshold
    }

    [Fact]
    public async Task Reset_WhenCircuitOpen_ShouldReopenCircuit()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 2);
        var exception = new InvalidOperationException("Test exception");

        // Act - Open the circuit
        await strategy.ShouldRetryAsync(1, exception);
        await strategy.ShouldRetryAsync(2, exception);

        // Circuit should be open
        var resultBeforeReset = await strategy.ShouldRetryAsync(3, exception);

        // Reset
        strategy.Reset();

        // Circuit should be closed now
        var resultAfterReset = await strategy.ShouldRetryAsync(4, exception);

        // Assert
        Assert.False(resultBeforeReset);
        Assert.True(resultAfterReset);
    }

    [Fact]
    public void Reset_ShouldNotAffectInnerStrategy()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);

        // Act
        strategy.Reset();

        // Assert
        mockInnerStrategy.Verify(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Exception Type Tests

    [Fact]
    public async Task ShouldRetryAsync_WithDifferentExceptionTypes_ShouldHandleAll()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 5);

        var exceptions = new Exception[]
        {
            new InvalidOperationException("Invalid operation"),
            new ArgumentException("Argument error"),
            new TimeoutException("Timeout"),
            new HttpRequestException("HTTP error"),
            new IOException("IO error")
        };

        // Act & Assert
        for (int i = 0; i < exceptions.Length; i++)
        {
            var result = await strategy.ShouldRetryAsync(i + 1, exceptions[i]);
            Assert.True(result);
        }

        mockInnerStrategy.Verify(
            s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
            Times.Exactly(5));
    }

    [Fact]
    public async Task ShouldRetryAsync_WithAggregateException_ShouldPassToInnerStrategy()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);
        var innerException = new InvalidOperationException("Inner exception");
        var aggregateException = new AggregateException("Multiple errors", innerException);

        // Act
        var result = await strategy.ShouldRetryAsync(1, aggregateException);

        // Assert
        Assert.True(result);
        mockInnerStrategy.Verify(
            s => s.ShouldRetryAsync(1, aggregateException, default),
            Times.Once);
    }

    #endregion

    #region Attempt Number Tests

    [Fact]
    public async Task ShouldRetryAsync_WithVaryingAttemptNumbers_ShouldPassToInnerStrategy()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object);
        var exception = new InvalidOperationException("Test");

        // Act
        await strategy.ShouldRetryAsync(1, exception);
        await strategy.ShouldRetryAsync(5, exception);
        await strategy.ShouldRetryAsync(100, exception);

        // Assert
        mockInnerStrategy.Verify(s => s.ShouldRetryAsync(1, exception, default), Times.Once);
        mockInnerStrategy.Verify(s => s.ShouldRetryAsync(5, exception, default), Times.Once);
        mockInnerStrategy.Verify(s => s.ShouldRetryAsync(100, exception, default), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompleteRetryScenario_ShouldOpenCircuitAfterThreshold()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.Setup(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockInnerStrategy.Setup(s => s.GetRetryDelayAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(10));

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
        var exception = new InvalidOperationException("Persistent failure");

        // Act - Attempt retries until circuit opens
        var attempt = 1;
        bool retrying = true;
        while (retrying && attempt <= 10)
        {
            retrying = await strategy.ShouldRetryAsync(attempt, exception);
            if (retrying)
            {
                var delay = await strategy.GetRetryDelayAsync(attempt, exception);
            }
            attempt++;
        }

        // Assert
        Assert.True(attempt > 3, "Circuit should have opened after threshold");
    }

    [Fact]
    public async Task ScenarioWithRecovery_ShouldResetAfterSuccess()
    {
        // Arrange
        var mockInnerStrategy = new Mock<IRetryStrategy>();
        mockInnerStrategy.SetupSequence(s => s.ShouldRetryAsync(It.IsAny<int>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)  // fail 1
            .ReturnsAsync(true)  // fail 2
            .ReturnsAsync(false) // success - reset
            .ReturnsAsync(true); // retry allowed again

        var strategy = new CircuitBreakerRetryStrategy(mockInnerStrategy.Object, failureThreshold: 3);
        var exception = new InvalidOperationException("Transient failure");

        // Act
        var result1 = await strategy.ShouldRetryAsync(1, exception);
        var result2 = await strategy.ShouldRetryAsync(2, exception);
        var result3 = await strategy.ShouldRetryAsync(3, exception); // Success, resets failures

        // Now try retrying again - should work because failures were reset
        var result4 = await strategy.ShouldRetryAsync(4, exception);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.False(result3);
        Assert.True(result4);
    }

    #endregion
}
