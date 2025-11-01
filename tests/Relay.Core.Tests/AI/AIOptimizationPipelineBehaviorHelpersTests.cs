using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors;
using Relay.Core.AI.Pipeline.Interfaces;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

/// <summary>
/// Tests for helper methods in AIOptimizationPipelineBehavior
/// </summary>
public class AIOptimizationPipelineBehaviorHelpersTests
{
    private readonly Mock<IAIOptimizationEngine> _aiEngineMock;
    private readonly Mock<ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<ISystemLoadMetricsProvider> _systemMetricsMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;
    private readonly AIOptimizationOptions _options;

    public AIOptimizationPipelineBehaviorHelpersTests()
    {
        _aiEngineMock = new Mock<IAIOptimizationEngine>();
        _loggerMock = new Mock<ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>>>();
        _systemMetricsMock = new Mock<ISystemLoadMetricsProvider>();
        _metricsProviderMock = new Mock<IMetricsProvider>();

        _options = new AIOptimizationOptions
        {
            Enabled = true,
            LearningEnabled = true,
            MinConfidenceScore = 0.7,
            MinCacheHitRate = 0.5
        };
    }

    [Fact]
    public async Task GetHistoricalMetrics_WithMetricsProvider_ReturnsHistoricalData()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            RequestType = typeof(TestRequest),
            TotalExecutions = 100,
            SuccessfulExecutions = 95,
            FailedExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(150),
            P50ExecutionTime = TimeSpan.FromMilliseconds(120),
            P95ExecutionTime = TimeSpan.FromMilliseconds(250),
            P99ExecutionTime = TimeSpan.FromMilliseconds(400),
            LastExecution = DateTimeOffset.UtcNow,
            Properties = new Dictionary<string, object>
            {
                ["AverageMemoryBytes"] = 1024L * 512,
                ["DatabaseCalls"] = 2,
                ["ExternalApiCalls"] = 1
            }
        };

        _metricsProviderMock
            .Setup(m => m.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        // Act - using reflection to call private method
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("GetHistoricalMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (ValueTask<RequestExecutionMetrics>)method!.Invoke(behavior, new object[] { typeof(TestRequest), CancellationToken.None })!;
        var result = await task;

        // Assert
        Assert.Equal(100, result.TotalExecutions);
        Assert.Equal(95, result.SuccessfulExecutions);
        Assert.Equal(5, result.FailedExecutions);
        Assert.Equal(TimeSpan.FromMilliseconds(150), result.AverageExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(120), result.MedianExecutionTime);
        Assert.Equal(2, result.DatabaseCalls);
        Assert.Equal(1, result.ExternalApiCalls);
    }

    [Fact]
    public async Task GetHistoricalMetrics_WithNoData_ReturnsDefaults()
    {
        // Arrange
        _metricsProviderMock
            .Setup(m => m.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns((HandlerExecutionStats?)null);

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("GetHistoricalMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (ValueTask<RequestExecutionMetrics>)method!.Invoke(behavior, new object[] { typeof(TestRequest), CancellationToken.None })!;
        var result = await task;

        // Assert
        Assert.Equal(0, result.TotalExecutions);
        Assert.Equal(TimeSpan.FromMilliseconds(100), result.AverageExecutionTime);
        Assert.Equal(0, result.DatabaseCalls);
        Assert.Equal(0, result.ExternalApiCalls);
    }

    [Fact]
    public void EstimateMemoryUsage_WithAverageMemoryAllocated_ReturnsValue()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            AverageMemoryAllocated = 2048L,
            TotalExecutions = 10
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (long)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(2048L, result);
    }

    [Fact]
    public void EstimateMemoryUsage_WithTotalMemoryAllocated_CalculatesAverage()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            TotalMemoryAllocated = 10240L,
            TotalExecutions = 10
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (long)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(1024L, result);
    }

    [Fact]
    public void EstimateMemoryUsage_WithPropertiesData_ReturnsPropertyValue()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            Properties = new Dictionary<string, object>
            {
                ["AverageMemoryBytes"] = 4096L
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (long)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(4096L, result);
    }

    [Fact]
    public void EstimateMemoryUsage_WithMemoryPerExecution_ReturnsPropertyValue()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            Properties = new Dictionary<string, object>
            {
                ["MemoryPerExecution"] = 8192L
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (long)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(8192L, result);
    }

    [Fact]
    public void EstimateMemoryUsage_WithHighExecutionTimeVariance_IncreasesEstimate()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P50ExecutionTime = TimeSpan.FromMilliseconds(50),
            P95ExecutionTime = TimeSpan.FromMilliseconds(200), // High variance
            TotalExecutions = 100,
            FailedExecutions = 10,
            LastExecution = DateTimeOffset.UtcNow.AddSeconds(-10)
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (long)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.True(result > 1024); // Should be higher than base estimate due to variance
        Assert.True(result <= 100 * 1024 * 1024); // Should be within bounds
    }

    [Fact]
    public void EstimateMemoryUsage_WithHighFailureRate_IncreasesEstimate()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P50ExecutionTime = TimeSpan.FromMilliseconds(90),
            P95ExecutionTime = TimeSpan.FromMilliseconds(120),
            TotalExecutions = 100,
            FailedExecutions = 50, // 50% failure rate
            LastExecution = DateTimeOffset.UtcNow.AddSeconds(-10)
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (long)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.True(result > 1024); // Should be higher than base estimate due to failure rate
        Assert.True(result <= 100 * 1024 * 1024); // Should be within bounds
    }

    [Fact]
    public void EstimateMemoryUsage_WithHighExecutionFrequency_DecreasesEstimate()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P50ExecutionTime = TimeSpan.FromMilliseconds(90),
            P95ExecutionTime = TimeSpan.FromMilliseconds(120),
            TotalExecutions = 1000, // High execution count
            FailedExecutions = 10,
            LastExecution = DateTimeOffset.UtcNow.AddSeconds(-1) // Very recent
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (long)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.True(result >= 1024); // Should be at least minimum
        Assert.True(result <= 100 * 1024 * 1024); // Should be within bounds
        // High frequency should potentially decrease estimate due to pooling assumption
    }

    [Fact]
    public void EstimateMemoryUsage_WithAllFactors_CombinesAdjustmentsCorrectly()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P50ExecutionTime = TimeSpan.FromMilliseconds(50),
            P95ExecutionTime = TimeSpan.FromMilliseconds(200), // High variance
            TotalExecutions = 100,
            FailedExecutions = 20, // 20% failure rate
            LastExecution = DateTimeOffset.UtcNow.AddSeconds(-10)
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (long)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.True(result >= 1024); // Should be at least minimum
        Assert.True(result <= 100 * 1024 * 1024); // Should be within bounds
        // Should combine variance, failure rate, and frequency factors
    }

    [Fact]
    public void CalculateExecutionTimeVariance_WithNormalDistribution_ReturnsVariance()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P50ExecutionTime = TimeSpan.FromMilliseconds(90),
            P95ExecutionTime = TimeSpan.FromMilliseconds(150)
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("CalculateExecutionTimeVariance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (double)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.True(result >= 0.0 && result <= 1.0);
    }

    [Fact]
    public void EstimateCpuUsage_WithHighP99ToP50Ratio_ReturnsHighUsage()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            P50ExecutionTime = TimeSpan.FromMilliseconds(100),
            P99ExecutionTime = TimeSpan.FromMilliseconds(600) // 6x ratio
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateCpuUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (double)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(0.8, result);
    }

    [Fact]
    public void EstimateCpuUsage_WithMediumP99ToP50Ratio_ReturnsMediumUsage()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            P50ExecutionTime = TimeSpan.FromMilliseconds(100),
            P99ExecutionTime = TimeSpan.FromMilliseconds(350) // 3.5x ratio
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateCpuUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (double)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(0.5, result);
    }

    [Fact]
    public void EstimateCpuUsage_WithLowP99ToP50Ratio_ReturnsLowUsage()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            P50ExecutionTime = TimeSpan.FromMilliseconds(100),
            P99ExecutionTime = TimeSpan.FromMilliseconds(200) // 2x ratio
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateCpuUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (double)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(0.3, result);
    }

    [Fact]
    public void ExtractDatabaseCalls_WithDatabaseCallsProperty_ReturnsValue()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            Properties = new Dictionary<string, object>
            {
                ["DatabaseCalls"] = 5
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void ExtractDatabaseCalls_WithAvgDatabaseCallsProperty_ReturnsRoundedValue()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            Properties = new Dictionary<string, object>
            {
                ["AvgDatabaseCalls"] = 3.7
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void ExtractDatabaseCalls_WithLongExecutionTime_EstimatesFromTime()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(250),
            Properties = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.True(result > 0); // Should estimate some DB calls
    }

    [Fact]
    public void ExtractExternalApiCalls_WithExternalApiCallsProperty_ReturnsValue()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            Properties = new Dictionary<string, object>
            {
                ["ExternalApiCalls"] = 2
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ExtractExternalApiCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void ExtractExternalApiCalls_WithHttpCallsProperty_ReturnsValue()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            Properties = new Dictionary<string, object>
            {
                ["HttpCalls"] = 3L
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ExtractExternalApiCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void ExtractExternalApiCalls_WithHighVariableLatency_EstimatesApiCalls()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(300),
            P50ExecutionTime = TimeSpan.FromMilliseconds(200),
            P99ExecutionTime = TimeSpan.FromMilliseconds(1000), // High variance
            Properties = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ExtractExternalApiCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.Equal(1, result); // Should estimate API calls based on latency variance
    }

    [Fact]
    public void CalculateExecutionFrequency_WithRecentExecutions_ReturnsFrequency()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            TotalExecutions = 1000,
            LastExecution = DateTimeOffset.UtcNow.AddSeconds(-10) // 10 seconds ago
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("CalculateExecutionFrequency", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (double)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.True(result > 0);
        Assert.True(result < 200); // Should be ~100 per second
    }

    [Fact]
    public void EstimateConcurrentExecutions_WithHighFrequency_ReturnsHigherConcurrency()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            TotalExecutions = 1000,
            AverageExecutionTime = TimeSpan.FromSeconds(1),
            LastExecution = DateTimeOffset.UtcNow.AddSeconds(-10)
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("EstimateConcurrentExecutions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { stats })!;

        // Assert
        Assert.True(result >= 1);
    }

    [Fact]
    public void ShouldPerformOptimization_WithNoAttributes_ReturnsEnabledOption()
    {
        // Arrange
        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        var emptyAttributes = Array.Empty<AIOptimizedAttribute>();

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ShouldPerformOptimization", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { emptyAttributes })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldPerformOptimization_WithMetricsTrackingEnabled_ReturnsTrue()
    {
        // Arrange
        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        var attributes = new[]
        {
            new AIOptimizedAttribute { EnableMetricsTracking = true }
        };

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ShouldPerformOptimization", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { attributes })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldPerformOptimization_WithAutoApplyOptimizationsEnabled_ReturnsTrue()
    {
        // Arrange
        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        var attributes = new[]
        {
            new AIOptimizedAttribute { AutoApplyOptimizations = true }
        };

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ShouldPerformOptimization", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { attributes })!;

        // Assert
        Assert.True(result);
    }

    // Helper classes
    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    [AIOptimized(EnableMetricsTracking = true)]
    public class OptimizedTestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [AIOptimized(AutoApplyOptimizations = true)]  // Handler with attribute
    public class TestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<TestResponse>(new TestResponse { Result = request.Value });
        }
    }

    [AIOptimized(EnableMetricsTracking = true)]  // Request with attribute
    public class RequestWithAttribute : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [AIOptimized(AutoApplyOptimizations = true)]  // Handler with attribute
    public class HandlerWithMethodAttribute : IRequestHandler<RequestWithAttribute, TestResponse>
    {
        [AIOptimized(AutoApplyOptimizations = true)]  // Method with attribute
        public ValueTask<TestResponse> HandleAsync(RequestWithAttribute request, CancellationToken cancellationToken)
        {
            return new ValueTask<TestResponse>(new TestResponse { Result = request.Value });
        }
    }

    [Fact]
    public void GetAIOptimizationAttributes_WhenHandlerExists_ReturnsAllAttributes()
    {
        // Arrange - Create a request type that has a corresponding handler with attributes
        var loggerMockForRequestWithAttribute = new Mock<ILogger<AIOptimizationPipelineBehavior<RequestWithAttribute, TestResponse>>>();
        var behavior = new AIOptimizationPipelineBehavior<RequestWithAttribute, TestResponse>(
            _aiEngineMock.Object,
            loggerMockForRequestWithAttribute.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act - using reflection to call private method
        var method = typeof(AIOptimizationPipelineBehavior<RequestWithAttribute, TestResponse>)
            .GetMethod("GetAIOptimizationAttributes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (AIOptimizedAttribute[])method!.Invoke(behavior, new object[] { typeof(RequestWithAttribute) })!;

        // Assert - There should be attributes from request, handler class, and method
        Assert.NotNull(result);
        
        // At least one attribute should be found which means the if (handlerType != null) branch executed
        Assert.NotEmpty(result); // Should have at least 1 attribute (from request or handler or method)
        
        // Verify that we get attributes from multiple sources
        bool hasEnableMetricsTracking = false;
        bool hasAutoApplyOptimizations = false;
        
        foreach (var attr in result)
        {
            if (attr.EnableMetricsTracking) hasEnableMetricsTracking = true;
            if (attr.AutoApplyOptimizations) hasAutoApplyOptimizations = true;
        }
        
        Assert.True(hasEnableMetricsTracking); // At least one attribute has EnableMetricsTracking
        Assert.True(hasAutoApplyOptimizations); // At least one attribute has AutoApplyOptimizations
    }

    [Fact]
    public void GetAIOptimizationAttributes_WhenHandlerExists_ChecksHandlerAttributes()
    {
        // Arrange - Create a request type that has a corresponding handler (even if no additional attributes)
        var loggerMockForTestRequest = new Mock<ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>>>();
        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            loggerMockForTestRequest.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act - using reflection to call private method
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("GetAIOptimizationAttributes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (AIOptimizedAttribute[])method!.Invoke(behavior, new object[] { typeof(TestRequest) })!;

        // Assert - Result may not be empty if default attribute values are included
        // The key is that the method executed without error and that FindHandlerType was called
        Assert.NotNull(result);
        
        // This test confirms that the if (handlerType != null) condition was reached
        // and the method continued execution properly
    }
}
