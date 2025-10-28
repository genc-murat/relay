using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors.Strategies;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Strategies;

/// <summary>
/// Tests for MemoryPoolingOptimizationStrategy implementation.
/// </summary>
public class MemoryPoolingOptimizationStrategyTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public MemoryPoolingOptimizationStrategyTests()
    {
        _loggerMock = new Mock<ILogger>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
    }

    [Fact]
    public async Task MemoryPoolingOptimizationStrategy_CanApplyAsync_WithNormalLoad_ReturnsTrue()
    {
        // Arrange
        var strategy = new MemoryPoolingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.MemoryPooling,
            ConfidenceScore = 0.4, // Lower threshold for memory optimizations
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            MemoryUtilization = 0.5 // Normal memory usage
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task MemoryPoolingOptimizationStrategy_CanApplyAsync_WithHighMemoryLoad_ReturnsFalse()
    {
        // Arrange
        var strategy = new MemoryPoolingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.MemoryPooling,
            ConfidenceScore = 0.4,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            MemoryUtilization = 0.96 // Critical memory usage
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MemoryPoolingOptimizationStrategy_CanApplyAsync_WithLowConfidence_ReturnsFalse()
    {
        // Arrange
        var strategy = new MemoryPoolingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.MemoryPooling,
            ConfidenceScore = 0.2, // Below threshold
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            MemoryUtilization = 0.5
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MemoryPoolingOptimizationStrategy_CanApplyAsync_WithVeryHighMemoryLoad_ReturnsFalse()
    {
        // Arrange
        var strategy = new MemoryPoolingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.MemoryPooling,
            ConfidenceScore = 0.4,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            MemoryUtilization = 0.98 // Extremely high memory usage
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MemoryPoolingOptimizationStrategy_ApplyAsync_WithAllParameters_UsesConfiguration()
    {
        // Arrange
        var strategy = new MemoryPoolingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.MemoryPooling,
            ConfidenceScore = 0.6,
            Parameters = new Dictionary<string, object>
            {
                ["PoolSize"] = 1024,
                ["EnableObjectPooling"] = true,
                ["PoolGrowthFactor"] = 2.0,
                ["MaxPoolSize"] = 8192,
                ["EnableMemoryTracking"] = true
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            MemoryUtilization = 0.4,
            AvailableMemory = 2048
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"pool_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("pool_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task MemoryPoolingOptimizationStrategy_ApplyAsync_WithLargePoolSize_ExecutesSuccessfully()
    {
        // Arrange
        var strategy = new MemoryPoolingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.MemoryPooling,
            ConfidenceScore = 0.5,
            Parameters = new Dictionary<string, object>
            {
                ["PoolSize"] = 4096,
                ["EnableLargeObjectHeap"] = true,
                ["PoolRetentionTime"] = TimeSpan.FromMinutes(10)
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            MemoryUtilization = 0.3,
            AvailableMemory = 4096
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"large_pool_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("large_pool_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task MemoryPoolingOptimizationStrategy_ApplyAsync_HandlesExceptions_Gracefully()
    {
        // Arrange
        var strategy = new MemoryPoolingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.MemoryPooling,
            ConfidenceScore = 0.5,
            Parameters = new Dictionary<string, object>
            {
                ["PoolSize"] = 512,
                ["ExceptionHandling"] = "Continue"
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            MemoryUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()))
            .Throws(new Exception("Memory pool failure"));

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "exception_handled" });

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert - Should still execute successfully despite metrics failure
        Assert.NotNull(result);
        Assert.Equal("exception_handled", result.Result);
    }
}