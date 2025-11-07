using System;
using Relay.Core.AI.CircuitBreaker.Metrics;
using Relay.Core.AI.CircuitBreaker.Options;
using Relay.Core.AI.CircuitBreaker.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI.CircuitBreaker.Strategies;

public class PercentageBasedCircuitBreakerStrategyTests
{
    [Fact]
    public void Constructor_WithDefaultParameter_ShouldUseDefaultThreshold()
    {
        // Arrange & Act
        var strategy = new PercentageBasedCircuitBreakerStrategy();

        // Assert
        Assert.Equal("PercentageBased", strategy.Name);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Constructor_WithValidFailureRateThreshold_ShouldInitializeCorrectly(double threshold)
    {
        // Arrange & Act
        var strategy = new PercentageBasedCircuitBreakerStrategy(threshold);

        // Assert
        Assert.Equal("PercentageBased", strategy.Name);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    public void Constructor_WithInvalidFailureRateThreshold_ShouldThrowArgumentException(double threshold)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new PercentageBasedCircuitBreakerStrategy(threshold));
    }

    [Fact]
    public void Name_ShouldReturnPercentageBased()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy();

        // Act & Assert
        Assert.Equal("PercentageBased", strategy.Name);
    }

    [Theory]
    [InlineData(0)]  // Less than minimum calls
    [InlineData(5)]  // Less than minimum calls
    [InlineData(9)]  // Less than minimum calls
    public void ShouldOpen_WithLessThanMinimumEffectiveCalls_ShouldReturnFalse(int effectiveCalls)
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.5);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = effectiveCalls,
            FailedCalls = effectiveCalls / 2,  // 50% failure rate
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldOpen_WithMinimumEffectiveCallsAndFailureRateBelowThreshold_ShouldReturnFalse()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.5);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 10,
            FailedCalls = 4,  // 40% failure rate, below 50% threshold
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldOpen_WithMinimumEffectiveCallsAndFailureRateEqualToThreshold_ShouldReturnTrue()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.5);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 10,
            FailedCalls = 5,  // 50% failure rate, equal to threshold
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldOpen_WithMinimumEffectiveCallsAndFailureRateAboveThreshold_ShouldReturnTrue()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.5);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 10,
            FailedCalls = 6,  // 60% failure rate, above 50% threshold
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldOpen_WithRejectedCalls_ShouldUseEffectiveCallsForMinimumCheck()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.5);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 15,
            FailedCalls = 8,  // 53% failure rate
            RejectedCalls = 6  // EffectiveCalls = 15 - 6 = 9, which is < 10
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result);  // Should return false because EffectiveCalls < 10
    }

    [Fact]
    public void ShouldOpen_WithRejectedCallsAndSufficientEffectiveCalls_ShouldUseFailureRate()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.5);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 20,
            FailedCalls = 12,  // 60% failure rate
            RejectedCalls = 5   // EffectiveCalls = 20 - 5 = 15, which is >= 10
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.True(result);  // Should return true because FailureRate >= threshold
    }

    [Fact]
    public void ShouldOpen_WithZeroTotalCalls_ShouldReturnFalse()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.5);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 0,
            FailedCalls = 0,
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldOpen_WithThresholdZero_ShouldOpenOnAnyFailure()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.0);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 10,
            FailedCalls = 1,  // 10% failure rate
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.True(result);  // Any failure should trigger with threshold 0.0
    }

    [Fact]
    public void ShouldOpen_WithThresholdOne_ShouldOnlyOpenOnCompleteFailure()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(1.0);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 10,
            FailedCalls = 9,  // 90% failure rate, below 100%
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result);  // Should not open unless 100% failure
    }

    [Fact]
    public void ShouldOpen_WithThresholdOneAndCompleteFailure_ShouldOpen()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(1.0);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 10,
            FailedCalls = 10,  // 100% failure rate
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.True(result);  // Should open on 100% failure
    }

    [Fact]
    public void ShouldOpen_WithFractionalFailureRate_ShouldHandlePrecisely()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.333);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 30,
            FailedCalls = 10,  // 33.333% failure rate
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.True(result);  // 0.333 >= 0.333
    }

    [Fact]
    public void ShouldOpen_WithLargeCallCounts_ShouldHandleCorrectly()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(0.5);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 10000,
            FailedCalls = 4999,  // 49.99% failure rate, below 50%
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result);  // 0.4999 < 0.5
    }

    [Theory]
    [InlineData(0, 0.5)]  // 0% failure rate
    [InlineData(1, 0.5)]  // 10% failure rate
    [InlineData(3, 0.5)]  // 30% failure rate
    [InlineData(5, 0.5)]  // 50% failure rate
    [InlineData(7, 0.5)]  // 70% failure rate
    [InlineData(10, 0.5)] // 100% failure rate
    public void ShouldOpen_WithExactlyTenEffectiveCalls_VariousFailureRates(int failedCalls, double threshold)
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy(threshold);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 10,
            FailedCalls = failedCalls,
            RejectedCalls = 0
        };
        var options = new AICircuitBreakerOptions();
        var expected = (double)failedCalls / 10 >= threshold;

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 1)]  // recentSuccesses < SuccessThreshold
    [InlineData(0, 3)]  // recentSuccesses < SuccessThreshold
    [InlineData(1, 3)]  // recentSuccesses < SuccessThreshold
    [InlineData(2, 3)]  // recentSuccesses < SuccessThreshold
    [InlineData(49, 50)] // recentSuccesses < SuccessThreshold
    public void ShouldClose_WithRecentSuccessesBelowThreshold_ShouldReturnFalse(int recentSuccesses, int successThreshold)
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = successThreshold };

        // Act
        var result = strategy.ShouldClose(recentSuccesses, 0, options);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(1, 1)]  // recentSuccesses == SuccessThreshold
    [InlineData(3, 3)]  // recentSuccesses == SuccessThreshold
    [InlineData(4, 3)]  // recentSuccesses > SuccessThreshold
    [InlineData(10, 3)] // recentSuccesses > SuccessThreshold
    [InlineData(100, 50)] // recentSuccesses > SuccessThreshold
    public void ShouldClose_WithRecentSuccessesAtOrAboveThreshold_ShouldReturnTrue(int recentSuccesses, int successThreshold)
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = successThreshold };

        // Act
        var result = strategy.ShouldClose(recentSuccesses, 0, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldClose_WithZeroSuccessThreshold_ShouldHandleEdgeCase()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = 0 };

        // Act
        var result = strategy.ShouldClose(0, 0, options);

        // Assert
        Assert.True(result);  // 0 >= 0 should be true
    }

    [Fact]
    public void ShouldClose_IgnoresRecentFailuresParameter()
    {
        // Arrange
        var strategy = new PercentageBasedCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = 3 };

        // Act
        var resultWithFailures = strategy.ShouldClose(3, 5, options);
        var resultWithoutFailures = strategy.ShouldClose(3, 0, options);

        // Assert
        Assert.True(resultWithFailures);
        Assert.True(resultWithoutFailures);
    }
}