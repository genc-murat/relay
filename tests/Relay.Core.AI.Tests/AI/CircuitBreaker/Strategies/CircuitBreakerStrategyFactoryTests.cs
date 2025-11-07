using Relay.Core.AI.CircuitBreaker.Options;
using Relay.Core.AI.CircuitBreaker.Strategies;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.CircuitBreaker.Strategies;

public class CircuitBreakerStrategyFactoryTests
{
    #region CreateStrategy Tests

    [Fact]
    public void CreateStrategy_Should_Return_StandardCircuitBreakerStrategy_For_Standard_Policy()
    {
        // Act
        var strategy = CircuitBreakerStrategyFactory.CreateStrategy(CircuitBreakerPolicy.Standard);

        // Assert
        Assert.IsType<StandardCircuitBreakerStrategy>(strategy);
        Assert.Equal("Standard", strategy.Name);
    }

    [Fact]
    public void CreateStrategy_Should_Return_PercentageBasedCircuitBreakerStrategy_For_PercentageBased_Policy()
    {
        // Act
        var strategy = CircuitBreakerStrategyFactory.CreateStrategy(CircuitBreakerPolicy.PercentageBased);

        // Assert
        Assert.IsType<PercentageBasedCircuitBreakerStrategy>(strategy);
        Assert.Equal("PercentageBased", strategy.Name);
    }

    [Fact]
    public void CreateStrategy_Should_Return_AdaptiveCircuitBreakerStrategy_For_Adaptive_Policy()
    {
        // Act
        var strategy = CircuitBreakerStrategyFactory.CreateStrategy(CircuitBreakerPolicy.Adaptive);

        // Assert
        Assert.IsType<AdaptiveCircuitBreakerStrategy>(strategy);
        Assert.Equal("Adaptive", strategy.Name);
    }

    [Fact]
    public void CreateStrategy_Should_Throw_ArgumentException_For_Invalid_Policy()
    {
        // Arrange
        var invalidPolicy = (CircuitBreakerPolicy)999;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CircuitBreakerStrategyFactory.CreateStrategy(invalidPolicy));

        Assert.Contains("Unsupported circuit breaker policy", exception.Message);
        Assert.Equal("policy", exception.ParamName);
    }

    #endregion

    #region CreatePercentageBasedStrategy Tests

    [Fact]
    public void CreatePercentageBasedStrategy_Should_Return_PercentageBasedCircuitBreakerStrategy_With_Default_Threshold()
    {
        // Act
        var strategy = CircuitBreakerStrategyFactory.CreatePercentageBasedStrategy(0.5);

        // Assert
        Assert.IsType<PercentageBasedCircuitBreakerStrategy>(strategy);
        Assert.Equal("PercentageBased", strategy.Name);
    }

    [Fact]
    public void CreatePercentageBasedStrategy_Should_Return_PercentageBasedCircuitBreakerStrategy_With_Custom_Threshold()
    {
        // Arrange
        double customThreshold = 0.75;

        // Act
        var strategy = CircuitBreakerStrategyFactory.CreatePercentageBasedStrategy(customThreshold);

        // Assert
        Assert.IsType<PercentageBasedCircuitBreakerStrategy>(strategy);
        Assert.Equal("PercentageBased", strategy.Name);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.001)]
    [InlineData(0.01)]
    [InlineData(0.1)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(0.9)]
    [InlineData(0.99)]
    [InlineData(0.999)]
    [InlineData(1.0)]
    public void CreatePercentageBasedStrategy_Should_Accept_Valid_Thresholds(double threshold)
    {
        // Act
        var strategy = CircuitBreakerStrategyFactory.CreatePercentageBasedStrategy(threshold);

        // Assert
        Assert.IsType<PercentageBasedCircuitBreakerStrategy>(strategy);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void CreatePercentageBasedStrategy_Should_Throw_ArgumentException_For_Invalid_Threshold(double threshold)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CircuitBreakerStrategyFactory.CreatePercentageBasedStrategy(threshold));

        Assert.Contains("Failure rate threshold must be between 0 and 1", exception.Message);
        Assert.Equal("failureRateThreshold", exception.ParamName);
    }

    #endregion

    #region CreateAdaptiveStrategy Tests

    [Fact]
    public void CreateAdaptiveStrategy_Should_Return_AdaptiveCircuitBreakerStrategy_With_Default_Parameters()
    {
        // Act
        var strategy = CircuitBreakerStrategyFactory.CreateAdaptiveStrategy(5.0, 0.3);

        // Assert
        Assert.IsType<AdaptiveCircuitBreakerStrategy>(strategy);
        Assert.Equal("Adaptive", strategy.Name);
    }

    [Fact]
    public void CreateAdaptiveStrategy_Should_Return_AdaptiveCircuitBreakerStrategy_With_Custom_Parameters()
    {
        // Arrange
        double baseFailureThreshold = 10.0;
        double loadSensitivity = 0.7;

        // Act
        var strategy = CircuitBreakerStrategyFactory.CreateAdaptiveStrategy(baseFailureThreshold, loadSensitivity);

        // Assert
        Assert.IsType<AdaptiveCircuitBreakerStrategy>(strategy);
        Assert.Equal("Adaptive", strategy.Name);
    }

    [Theory]
    [InlineData(0.1, 0.0)]
    [InlineData(1.0, 0.0)]
    [InlineData(5.0, 0.0)]
    [InlineData(10.0, 0.0)]
    [InlineData(100.0, 0.0)]
    [InlineData(1.0, 0.001)]
    [InlineData(1.0, 0.1)]
    [InlineData(5.0, 0.5)]
    [InlineData(10.0, 0.9)]
    [InlineData(10.0, 0.999)]
    [InlineData(10.0, 1.0)]
    public void CreateAdaptiveStrategy_Should_Accept_Valid_Parameters(double baseFailureThreshold, double loadSensitivity)
    {
        // Act
        var strategy = CircuitBreakerStrategyFactory.CreateAdaptiveStrategy(baseFailureThreshold, loadSensitivity);

        // Assert
        Assert.IsType<AdaptiveCircuitBreakerStrategy>(strategy);
    }

    [Theory]
    [InlineData(0.0, 0.5)]
    [InlineData(-1.0, 0.5)]
    [InlineData(-5.0, 0.5)]
    public void CreateAdaptiveStrategy_Should_Throw_ArgumentException_For_Invalid_BaseFailureThreshold(double baseFailureThreshold, double loadSensitivity)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CircuitBreakerStrategyFactory.CreateAdaptiveStrategy(baseFailureThreshold, loadSensitivity));

        Assert.Contains("Base failure threshold must be greater than 0", exception.Message);
        Assert.Equal("baseFailureThreshold", exception.ParamName);
    }

    [Theory]
    [InlineData(5.0, -0.1)]
    [InlineData(5.0, 1.1)]
    [InlineData(5.0, 2.0)]
    public void CreateAdaptiveStrategy_Should_Throw_ArgumentException_For_Invalid_LoadSensitivity(double baseFailureThreshold, double loadSensitivity)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CircuitBreakerStrategyFactory.CreateAdaptiveStrategy(baseFailureThreshold, loadSensitivity));

        Assert.Contains("Load sensitivity must be between 0 and 1", exception.Message);
        Assert.Equal("loadSensitivity", exception.ParamName);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Factory_Methods_Should_Return_Different_Instances()
    {
        // Act
        var strategy1 = CircuitBreakerStrategyFactory.CreateStrategy(CircuitBreakerPolicy.Standard);
        var strategy2 = CircuitBreakerStrategyFactory.CreateStrategy(CircuitBreakerPolicy.Standard);

        // Assert
        Assert.NotSame(strategy1, strategy2);
    }

    [Fact]
    public void CreatePercentageBasedStrategy_Should_Return_Different_Instances_With_Different_Thresholds()
    {
        // Act
        var strategy1 = CircuitBreakerStrategyFactory.CreatePercentageBasedStrategy(0.3);
        var strategy2 = CircuitBreakerStrategyFactory.CreatePercentageBasedStrategy(0.7);

        // Assert
        Assert.NotSame(strategy1, strategy2);
        Assert.IsType<PercentageBasedCircuitBreakerStrategy>(strategy1);
        Assert.IsType<PercentageBasedCircuitBreakerStrategy>(strategy2);
    }

    [Fact]
    public void CreateAdaptiveStrategy_Should_Return_Different_Instances_With_Different_Parameters()
    {
        // Act
        var strategy1 = CircuitBreakerStrategyFactory.CreateAdaptiveStrategy(3.0, 0.2);
        var strategy2 = CircuitBreakerStrategyFactory.CreateAdaptiveStrategy(7.0, 0.8);

        // Assert
        Assert.NotSame(strategy1, strategy2);
        Assert.IsType<AdaptiveCircuitBreakerStrategy>(strategy1);
        Assert.IsType<AdaptiveCircuitBreakerStrategy>(strategy2);
    }

    #endregion
}