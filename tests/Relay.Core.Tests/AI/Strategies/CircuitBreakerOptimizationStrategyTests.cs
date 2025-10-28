using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors.Strategies;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Strategies;

/// <summary>
/// Tests for CircuitBreakerOptimizationStrategy implementation.
/// </summary>
public class CircuitBreakerOptimizationStrategyTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public CircuitBreakerOptimizationStrategyTests()
    {
        _loggerMock = new Mock<ILogger>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
    }

    [Fact]
    public async Task CircuitBreakerOptimizationStrategy_CanApplyAsync_WithStressConditions_ReturnsTrue()
    {
        // Arrange
        var strategy = new CircuitBreakerOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.CircuitBreaker,
            ConfidenceScore = 0.5,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            ErrorRate = 0.1, // High error rate
            CpuUtilization = 0.9 // High CPU load
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CircuitBreakerOptimizationStrategy_CanApplyAsync_WithLowErrorRateAndNormalCpu_ReturnsFalse()
    {
        // Arrange
        var strategy = new CircuitBreakerOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.CircuitBreaker,
            ConfidenceScore = 0.5,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            ErrorRate = 0.01, // Low error rate
            CpuUtilization = 0.5 // Normal CPU load
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CircuitBreakerOptimizationStrategy_CanApplyAsync_WithExtremeLoad_ReturnsTrue()
    {
        // Arrange
        var strategy = new CircuitBreakerOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.CircuitBreaker,
            ConfidenceScore = 0.5,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            ErrorRate = 0.2, // Very high error rate
            CpuUtilization = 0.95, // Extreme CPU load
            MemoryUtilization = 0.9
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CircuitBreakerOptimizationStrategy_CanApplyAsync_WithBoundaryErrorRates_ReturnsExpectedResults()
    {
        // Arrange
        var strategy = new CircuitBreakerOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.CircuitBreaker,
            ConfidenceScore = 0.5,
            Parameters = new Dictionary<string, object>()
        };

        // Test with error rate exactly at threshold (0.05)
        var systemLoadAtThreshold = new SystemLoadMetrics
        {
            ErrorRate = 0.05,
            CpuUtilization = 0.8
        };

        // Test with error rate below threshold
        var systemLoadBelowThreshold = new SystemLoadMetrics
        {
            ErrorRate = 0.04,
            CpuUtilization = 0.8
        };

        // Act
        var resultAtThreshold = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoadAtThreshold, CancellationToken.None);
        var resultBelowThreshold = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoadBelowThreshold, CancellationToken.None);

        // Assert
        Assert.False(resultAtThreshold); // ErrorRate == 0.05 is not > 0.05
        Assert.False(resultBelowThreshold);
    }

    [Fact]
    public async Task CircuitBreakerOptimizationStrategy_ApplyAsync_WithCustomThresholds_HandlesFailures()
    {
        // Arrange
        var strategy = new CircuitBreakerOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.CircuitBreaker,
            ConfidenceScore = 0.6,
            Parameters = new Dictionary<string, object>
            {
                ["FailureThreshold"] = 5,
                ["RecoveryTimeout"] = TimeSpan.FromSeconds(30),
                ["MonitoringWindow"] = TimeSpan.FromMinutes(1)
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            ErrorRate = 0.1,
            CpuUtilization = 0.8
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"circuit_breaker_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("circuit_breaker_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CircuitBreakerOptimizationStrategy_ApplyAsync_WithFailingHandler_HandlesGracefully()
    {
        // Arrange
        var strategy = new CircuitBreakerOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.CircuitBreaker,
            ConfidenceScore = 0.6,
            Parameters = new Dictionary<string, object>
            {
                ["FailureThreshold"] = 3,
                ["ExpectedExceptionTypes"] = new[] { typeof(TimeoutException), typeof(HttpRequestException) }
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            ErrorRate = 0.1,
            CpuUtilization = 0.8
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        RequestHandlerDelegate<TestResponse> next = () =>
            throw new TimeoutException("Circuit breaker test failure");

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await optimizedHandler());

        // Assert
        Assert.Equal("Circuit breaker test failure", exception.Message);
    }
}