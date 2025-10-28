using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors.Strategies;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Strategies;

/// <summary>
/// Tests for BaseOptimizationStrategy functionality.
/// </summary>
public class BaseOptimizationStrategyTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public BaseOptimizationStrategyTests()
    {
        _loggerMock = new Mock<ILogger>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
    }

    [Fact]
    public void BaseOptimizationStrategy_GetParameter_WithExistingParameter_ReturnsValue()
    {
        // Arrange
        var strategy = new TestableBaseOptimizationStrategy(_loggerMock.Object, _metricsProviderMock.Object);
        var recommendation = new OptimizationRecommendation
        {
            Parameters = new Dictionary<string, object> { ["TestParam"] = "testValue" }
        };

        // Act
        var result = strategy.GetParameter(recommendation, "TestParam", "default");

        // Assert
        Assert.Equal("testValue", result);
    }

    [Fact]
    public void BaseOptimizationStrategy_GetParameter_WithMissingParameter_ReturnsDefault()
    {
        // Arrange
        var strategy = new TestableBaseOptimizationStrategy(_loggerMock.Object, _metricsProviderMock.Object);
        var recommendation = new OptimizationRecommendation
        {
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = strategy.GetParameter(recommendation, "MissingParam", "default");

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void BaseOptimizationStrategy_GetParameter_WithConvertibleType_ConvertsValue()
    {
        // Arrange
        var strategy = new TestableBaseOptimizationStrategy(_loggerMock.Object, _metricsProviderMock.Object);
        var recommendation = new OptimizationRecommendation
        {
            Parameters = new Dictionary<string, object> { ["IntParam"] = "42" }
        };

        // Act
        var result = strategy.GetParameter<int>(recommendation, "IntParam", 0);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void BaseOptimizationStrategy_MeetsConfidenceThreshold_WithSufficientConfidence_ReturnsTrue()
    {
        // Arrange
        var strategy = new TestableBaseOptimizationStrategy(_loggerMock.Object, _metricsProviderMock.Object);
        var recommendation = new OptimizationRecommendation
        {
            ConfidenceScore = 0.8
        };

        // Act
        var result = strategy.MeetsConfidenceThreshold(recommendation, 0.7);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void BaseOptimizationStrategy_MeetsConfidenceThreshold_WithInsufficientConfidence_ReturnsFalse()
    {
        // Arrange
        var strategy = new TestableBaseOptimizationStrategy(_loggerMock.Object, _metricsProviderMock.Object);
        var recommendation = new OptimizationRecommendation
        {
            ConfidenceScore = 0.5
        };

        // Act
        var result = strategy.MeetsConfidenceThreshold(recommendation, 0.7);

        // Assert
        Assert.False(result);
    }
}

/// <summary>
/// Testable implementation of BaseOptimizationStrategy for testing protected methods.
/// </summary>
public class TestableBaseOptimizationStrategy : BaseOptimizationStrategy<TestRequest, TestResponse>
{
    public TestableBaseOptimizationStrategy(ILogger logger, IMetricsProvider? metricsProvider = null)
        : base(logger, metricsProvider)
    {
    }

    public override OptimizationStrategy StrategyType => OptimizationStrategy.Custom;

    public override async ValueTask<bool> CanApplyAsync(TestRequest request, OptimizationRecommendation recommendation, SystemLoadMetrics systemLoad, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return true;
    }

    public override async ValueTask<RequestHandlerDelegate<TestResponse>> ApplyAsync(TestRequest request, RequestHandlerDelegate<TestResponse> next, OptimizationRecommendation recommendation, SystemLoadMetrics systemLoad, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return next;
    }

    // Expose protected methods for testing
    public new T GetParameter<T>(OptimizationRecommendation recommendation, string parameterName, T defaultValue)
    {
        return base.GetParameter(recommendation, parameterName, defaultValue);
    }

    public new bool MeetsConfidenceThreshold(OptimizationRecommendation recommendation, double threshold)
    {
        return base.MeetsConfidenceThreshold(recommendation, threshold);
    }
}