using Relay.Core.AI.CircuitBreaker.Metrics;
using Relay.Core.AI.CircuitBreaker.Options;
using Relay.Core.AI.CircuitBreaker.Strategies;
using System;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AdaptiveCircuitBreakerStrategyTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var strategy = new AdaptiveCircuitBreakerStrategy(5.0, 0.3);

        // Assert
        Assert.Equal("Adaptive", strategy.Name);
    }

    [Fact]
    public void Constructor_WithDefaultParameters_ShouldUseDefaults()
    {
        // Arrange & Act
        var strategy = new AdaptiveCircuitBreakerStrategy();

        // Assert
        Assert.Equal("Adaptive", strategy.Name);
    }

    [Fact]
    public void Constructor_WithInvalidBaseFailureThreshold_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new AdaptiveCircuitBreakerStrategy(0));
        Assert.Throws<ArgumentException>(() => new AdaptiveCircuitBreakerStrategy(-1));
    }

    [Fact]
    public void Constructor_WithInvalidLoadSensitivity_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new AdaptiveCircuitBreakerStrategy(5, -0.1));
        Assert.Throws<ArgumentException>(() => new AdaptiveCircuitBreakerStrategy(5, 1.1));
    }

    [Theory]
    [InlineData(0.1, 0.3)]  // Very small positive baseFailureThreshold
    [InlineData(1000, 0.3)] // Large baseFailureThreshold
    [InlineData(5, 0.0)]   // Minimum loadSensitivity
    [InlineData(5, 1.0)]   // Maximum loadSensitivity
    [InlineData(0.1, 0.0)] // Both boundary values
    [InlineData(1000, 1.0)] // Both extreme values
    public void Constructor_WithBoundaryValues_ShouldInitializeCorrectly(double baseFailureThreshold, double loadSensitivity)
    {
        // Arrange & Act
        var strategy = new AdaptiveCircuitBreakerStrategy(baseFailureThreshold, loadSensitivity);

        // Assert
        Assert.Equal("Adaptive", strategy.Name);
    }

    [Theory]
    [InlineData(0.0)]  // Exactly zero
    [InlineData(-0.1)] // Negative
    [InlineData(-1.0)] // More negative
    public void Constructor_WithInvalidBaseFailureThresholdValues_ShouldThrowArgumentException(double invalidThreshold)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AdaptiveCircuitBreakerStrategy(invalidThreshold));
        Assert.Contains("Base failure threshold must be greater than 0", exception.Message);
        Assert.Equal("baseFailureThreshold", exception.ParamName);
    }

    [Theory]
    [InlineData(-0.1)] // Negative
    [InlineData(-1.0)] // More negative
    [InlineData(1.1)]  // Above maximum
    [InlineData(2.0)]  // Well above maximum
    public void Constructor_WithInvalidLoadSensitivityValues_ShouldThrowArgumentException(double invalidSensitivity)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AdaptiveCircuitBreakerStrategy(5, invalidSensitivity));
        Assert.Contains("Load sensitivity must be between 0 and 1", exception.Message);
        Assert.Equal("loadSensitivity", exception.ParamName);
    }

    [Theory]
    [InlineData(1, 1, 1, true)]  // SuccessThreshold=1, threshold=1, recentSuccesses=1 >= 1
    [InlineData(1, 0, 1, false)] // SuccessThreshold=1, threshold=1, recentSuccesses=0 < 1
    [InlineData(2, 1, 1, true)]  // SuccessThreshold=2, threshold=1, recentSuccesses=1 >= 1
    [InlineData(2, 0, 1, false)] // SuccessThreshold=2, threshold=1, recentSuccesses=0 < 1
    [InlineData(3, 2, 1, true)]  // SuccessThreshold=3, threshold=2, recentSuccesses=2 >= 2
    [InlineData(3, 1, 1, false)] // SuccessThreshold=3, threshold=2, recentSuccesses=1 < 2
    [InlineData(4, 3, 1, true)]  // SuccessThreshold=4, threshold=3, recentSuccesses=3 >= 3
    [InlineData(4, 2, 1, false)] // SuccessThreshold=4, threshold=3, recentSuccesses=2 < 3
    public void ShouldClose_WithVariousSuccessThresholds_ShouldReturnCorrectResult(
        int successThreshold, int recentSuccesses, int recentFailures, bool expected)
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = successThreshold };

        // Act
        var result = strategy.ShouldClose(recentSuccesses, recentFailures, options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShouldClose_WithZeroRecentSuccesses_ShouldReturnFalse()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = 3 };

        // Act
        var result = strategy.ShouldClose(0, 5, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldClose_WithHighRecentFailures_ShouldStillCheckSuccesses()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = 3 };

        // Act
        var result = strategy.ShouldClose(2, 100, options);

        // Assert
        Assert.True(result); // 2 >= Math.Max(1, 3-1) = 2
    }

    [Fact]
    public void ShouldClose_WithMinimumSuccessThreshold_ShouldRequireOneSuccess()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = 1 };

        // Act
        var result = strategy.ShouldClose(1, 0, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldClose_WithHighSuccessThreshold_ShouldRequireMoreSuccesses()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = 10 };

        // Act
        var result = strategy.ShouldClose(9, 0, options);

        // Assert
        Assert.True(result); // 9 >= Math.Max(1, 10-1) = 9
    }

    [Fact]
    public void ShouldClose_WithBoundaryValues_ShouldHandleCorrectly()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy();
        var options = new AICircuitBreakerOptions { SuccessThreshold = 2 };

        // Act & Assert
        Assert.True(strategy.ShouldClose(1, 0, options));  // 1 >= 1
        Assert.False(strategy.ShouldClose(0, 0, options)); // 0 < 1
    }

    [Theory]
    [InlineData(5, 0.3, 1.0, 0.0, 4, false)]  // Perfect availability, no failures, 4 consecutive failures < threshold (5)
    [InlineData(5, 0.3, 1.0, 0.0, 5, true)]   // Perfect availability, no failures, 5 consecutive failures >= threshold (5)
    [InlineData(5, 0.3, 0.5, 0.0, 3, false)]  // 50% availability, 3 consecutive failures < adjusted threshold (4.25)
    [InlineData(5, 0.3, 0.5, 0.0, 4, false)]  // 50% availability, 4 consecutive failures < adjusted threshold (4.25)
    [InlineData(5, 0.3, 0.5, 0.0, 5, true)]   // 50% availability, 5 consecutive failures >= adjusted threshold (4.25)
    [InlineData(5, 0.3, 1.0, 0.5, 3, false)]  // High failure rate, 3 consecutive failures < adjusted threshold (3.5)
    [InlineData(5, 0.3, 1.0, 0.5, 5, true)]   // High failure rate, 5 consecutive failures >= adjusted threshold (3.5)
    [InlineData(5, 0.3, 0.5, 0.5, 2, false)]  // Both low availability and high failure rate, 2 consecutive failures < adjusted threshold (3.5)
    [InlineData(5, 0.3, 0.5, 0.5, 4, true)]   // Both low availability and high failure rate, 4 consecutive failures >= adjusted threshold (3.5)
    [InlineData(5, 0.3, 0.0, 0.0, 4, true)]   // 0% availability, 4 consecutive failures >= adjusted threshold (3.5)
    [InlineData(5, 0.3, 0.0, 0.5, 3, true)]   // 0% availability + high failure rate, 3 consecutive failures >= adjusted threshold (2.75)
    public void ShouldOpen_WithVariousMetrics_ShouldReturnCorrectResult(
        double baseFailureThreshold, double loadSensitivity, double availability, double failureRate,
        int consecutiveFailures, bool expected)
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy(baseFailureThreshold, loadSensitivity);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 100,
            SuccessfulCalls = (long)(availability * 100),
            FailedCalls = (long)(failureRate * 100),
            ConsecutiveFailures = consecutiveFailures
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShouldOpen_WithZeroConsecutiveFailures_ShouldReturnFalse()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy();
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 100,
            SuccessfulCalls = 50,
            FailedCalls = 50,
            ConsecutiveFailures = 0
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldOpen_WithHighLoadSensitivity_ShouldBeMoreAggressive()
    {
        // Arrange
        var lowSensitivityStrategy = new AdaptiveCircuitBreakerStrategy(5, 0.1);
        var highSensitivityStrategy = new AdaptiveCircuitBreakerStrategy(5, 0.9);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 100,
            SuccessfulCalls = 20, // 20% availability
            FailedCalls = 80,     // 80% failure rate
            ConsecutiveFailures = 2
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var lowSensitivityResult = lowSensitivityStrategy.ShouldOpen(metrics, options);
        var highSensitivityResult = highSensitivityStrategy.ShouldOpen(metrics, options);

        // Assert
        // High sensitivity should be more aggressive (lower threshold), so should open with fewer failures
        Assert.True(highSensitivityResult);
        // Low sensitivity might not open with just 2 failures
    }

    [Fact]
    public void ShouldOpen_WithPerfectMetrics_ShouldUseBaseThreshold()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy(5, 0.3);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 100,
            SuccessfulCalls = 100, // 100% availability
            FailedCalls = 0,       // 0% failure rate
            ConsecutiveFailures = 4 // Just below base threshold
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result); // 4 < 5 (base threshold when metrics are perfect)
    }

    [Fact]
    public void ShouldOpen_WithWorstMetrics_ShouldUseMinimumThreshold()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy(10, 1.0); // High sensitivity
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 100,
            SuccessfulCalls = 0,   // 0% availability
            FailedCalls = 100,     // 100% failure rate
            ConsecutiveFailures = 1 // At minimum threshold
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.True(result); // Should open at minimum threshold of 1
    }

    [Fact]
    public void ShouldOpen_WithNoCalls_ShouldUseBaseThreshold()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy(5, 0.3);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 0,
            SuccessfulCalls = 0,
            FailedCalls = 0,
            ConsecutiveFailures = 4
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result); // 4 < 5 (base threshold when no calls yet)
    }

    [Fact]
    public void ShouldOpen_WithNoCalls_AndConsecutiveFailuresAtThreshold_ShouldReturnTrue()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy(5, 0.3);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 0,
            SuccessfulCalls = 0,
            FailedCalls = 0,
            ConsecutiveFailures = 5
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.True(result); // 5 >= 5 (base threshold when no calls yet)
    }

    [Fact]
    public void ShouldOpen_WithRejectedCalls_ShouldAdjustAvailabilityCorrectly()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy(5, 0.3);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 100,
            SuccessfulCalls = 60,
            FailedCalls = 20,
            RejectedCalls = 20, // EffectiveCalls = 80, Availability = 60/80 = 0.75
            ConsecutiveFailures = 3
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result); // With availability 0.75, adjustment = (1-0.75)*0.3 = 0.075, threshold = 5 * (1-0.075) = 4.625, 3 < 4.625
    }

    [Fact]
    public void ShouldOpen_WithLargeCallCounts_ShouldHandleCorrectly()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy(5, 0.3);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 1000000,
            SuccessfulCalls = 800000,
            FailedCalls = 200000,
            ConsecutiveFailures = 4
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.False(result); // With high availability (0.8), threshold should be close to base, 4 < 5
    }

    [Fact]
    public void ShouldOpen_WithFractionalBaseThreshold_ShouldAdjustCorrectly()
    {
        // Arrange
        var strategy = new AdaptiveCircuitBreakerStrategy(2.5, 0.5);
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 100,
            SuccessfulCalls = 50,
            FailedCalls = 50,
            ConsecutiveFailures = 2
        };
        var options = new AICircuitBreakerOptions();

        // Act
        var result = strategy.ShouldOpen(metrics, options);

        // Assert
        Assert.True(result); // Availability=0.5, FailureRate=0.5, adjustment = (1-0.5)*0.5 + 0.5*0.5 = 0.25+0.25=0.5, threshold = 2.5 * (1-0.5) = 1.25, 2 >= 1.25
    }
}