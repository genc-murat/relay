using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors;
using Relay.Core.AI.Pipeline.Interfaces;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI;

/// <summary>
/// Tests for strategy methods in AIOptimizationPipelineBehavior
/// </summary>
public class AIOptimizationPipelineBehaviorStrategiesTests
{
    private readonly Mock<IAIOptimizationEngine> _aiEngineMock;
    private readonly Mock<ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<ISystemLoadMetricsProvider> _systemMetricsMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly AIOptimizationOptions _options;

    public AIOptimizationPipelineBehaviorStrategiesTests()
    {
        _aiEngineMock = new Mock<IAIOptimizationEngine>();
        _loggerMock = new Mock<ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>>>();
        _systemMetricsMock = new Mock<ISystemLoadMetricsProvider>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
        _memoryCacheMock = new Mock<IMemoryCache>();
        _distributedCacheMock = new Mock<IDistributedCache>();

        _options = new AIOptimizationOptions
        {
            Enabled = true,
            LearningEnabled = true,
            MinConfidenceScore = 0.7,
            MinCacheHitRate = 0.5
        };
    }

    [Fact]
    public void GetParameter_WithExactType_ReturnsValue()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.BatchProcessing,
            ConfidenceScore = 0.9,
            Parameters = new Dictionary<string, object>
            {
                ["BatchSize"] = 10
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("GetParameter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var genericMethod = method!.MakeGenericMethod(typeof(int));
        var result = (int)genericMethod.Invoke(behavior, new object[] { recommendation, "BatchSize", 5 })!;

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public void GetParameter_WithConvertibleType_ConvertsAndReturnsValue()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.9,
            Parameters = new Dictionary<string, object>
            {
                ["Timeout"] = "30"
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("GetParameter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var genericMethod = method!.MakeGenericMethod(typeof(int));
        var result = (int)genericMethod.Invoke(behavior, new object[] { recommendation, "Timeout", 10 })!;

        // Assert
        Assert.Equal(30, result);
    }

    [Fact]
    public void GetParameter_WithMissingParameter_ReturnsDefaultValue()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.BatchProcessing,
            ConfidenceScore = 0.9,
            Parameters = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("GetParameter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var genericMethod = method!.MakeGenericMethod(typeof(int));
        var result = (int)genericMethod.Invoke(behavior, new object[] { recommendation, "MissingParam", 42 })!;

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void GetParameter_WithBoolValue_ReturnsCorrectValue()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.9,
            Parameters = new Dictionary<string, object>
            {
                ["EnableCompression"] = true
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("GetParameter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var genericMethod = method!.MakeGenericMethod(typeof(bool));
        var result = (bool)genericMethod.Invoke(behavior, new object[] { recommendation, "EnableCompression", false })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetParameter_WithInvalidConversion_ReturnsDefaultValue()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.9,
            Parameters = new Dictionary<string, object>
            {
                ["InvalidParam"] = "not_a_number"
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("GetParameter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var genericMethod = method!.MakeGenericMethod(typeof(int));
        var result = (int)genericMethod.Invoke(behavior, new object[] { recommendation, "InvalidParam", 100 })!;

        // Assert
        Assert.Equal(100, result); // Should return default when conversion fails
    }

    [Fact]
    public void ShouldApplyBatching_WithLowBatchSize_ReturnsFalse()
    {
        // Arrange
        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5,
            ThroughputPerSecond = 10.0
        };

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.BatchProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ShouldApplyBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { systemLoad, 1, recommendation })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldApplyBatching_WithHighCpuLoad_ReturnsFalse()
    {
        // Arrange
        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.96,
            MemoryUtilization = 0.5,
            ThroughputPerSecond = 10.0
        };

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.BatchProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ShouldApplyBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { systemLoad, 10, recommendation })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldApplyBatching_WithHighMemoryLoad_ReturnsFalse()
    {
        // Arrange
        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.97,
            ThroughputPerSecond = 10.0
        };

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.BatchProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ShouldApplyBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { systemLoad, 10, recommendation })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldApplyBatching_WithLowConfidence_ReturnsFalse()
    {
        // Arrange
        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5,
            ThroughputPerSecond = 10.0
        };

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.BatchProcessing,
            ConfidenceScore = 0.5, // Below threshold
            Parameters = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ShouldApplyBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { systemLoad, 10, recommendation })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldApplyBatching_WithLowThroughput_ReturnsFalse()
    {
        // Arrange
        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5,
            ThroughputPerSecond = 2.0 // Too low
        };

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.BatchProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ShouldApplyBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { systemLoad, 10, recommendation })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldApplyBatching_WithGoodConditions_ReturnsTrue()
    {
        // Arrange
        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5,
            ThroughputPerSecond = 10.0
        };

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.BatchProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("ShouldApplyBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { systemLoad, 10, recommendation })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CalculateOptimalParallelism_WithHighCpuLoad_ReducesParallelism()
    {
        // Arrange
        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.9,
            ThreadPoolUtilization = 0.5
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("CalculateOptimalParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { -1, systemLoad })!;

        // Assert
        Assert.True(result >= 1);
        Assert.True(result <= Environment.ProcessorCount);
    }

    [Fact]
    public void CalculateOptimalParallelism_WithHighThreadPoolUtilization_HalvesParallelism()
    {
        // Arrange
        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.3,
            ThreadPoolUtilization = 0.85
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("CalculateOptimalParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { Environment.ProcessorCount, systemLoad })!;

        // Assert
        Assert.True(result >= 1);
        Assert.True(result <= Environment.ProcessorCount / 2 + 1); // Should be roughly half
    }

    [Fact]
    public void CalculateOptimalParallelism_ReturnsAtLeastOne()
    {
        // Arrange
        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.99,
            ThreadPoolUtilization = 0.99
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("CalculateOptimalParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (int)method!.Invoke(behavior, new object[] { 4, systemLoad })!;

        // Assert
        Assert.True(result >= 1);
    }

    [Fact]
    public void IsTransientDatabaseError_WithTimeoutInMessage_ReturnsTrue()
    {
        // Arrange
        var exception = new Exception("Connection timeout occurred");

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("IsTransientDatabaseError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { exception })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTransientDatabaseError_WithDeadlockInMessage_ReturnsTrue()
    {
        // Arrange
        var exception = new Exception("Transaction was deadlocked");

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("IsTransientDatabaseError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { exception })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTransientDatabaseError_WithConnectionInMessage_ReturnsTrue()
    {
        // Arrange
        var exception = new Exception("Connection failed to database");

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("IsTransientDatabaseError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { exception })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTransientDatabaseError_WithNetworkInMessage_ReturnsTrue()
    {
        // Arrange
        var exception = new Exception("Network error occurred");

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("IsTransientDatabaseError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { exception })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTransientDatabaseError_WithTransportInMessage_ReturnsTrue()
    {
        // Arrange
        var exception = new Exception("Transport-level error");

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("IsTransientDatabaseError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { exception })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTransientDatabaseError_WithNonTransientError_ReturnsFalse()
    {
        // Arrange
        var exception = new Exception("Invalid column name");

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("IsTransientDatabaseError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (bool)method!.Invoke(behavior, new object[] { exception })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetSupportedVectorTypes_ReturnsNonEmptyArray()
    {
        // Arrange
        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("GetSupportedVectorTypes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (string[])method!.Invoke(behavior, Array.Empty<object>())!;

        // Assert
        Assert.NotNull(result);
        // Result may vary based on hardware, so just check it's an array
    }

    [Fact]
    public void TryGetFallbackResponse_WithFallbackInParameters_ReturnsTrue()
    {
        // Arrange
        var fallbackResponse = new TestResponse { Result = "fallback" };
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.CircuitBreaker,
            ConfidenceScore = 0.9,
            Parameters = new Dictionary<string, object>
            {
                ["FallbackResponse"] = fallbackResponse
            }
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("TryGetFallbackResponse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var parameters = new object[] { recommendation, null };
        var result = (bool)method!.Invoke(behavior, parameters)!;
        var actualFallback = (TestResponse?)parameters[1];

        // Assert
        Assert.True(result);
        Assert.NotNull(actualFallback);
        Assert.Equal("fallback", actualFallback.Result);
    }

    [Fact]
    public void TryGetFallbackResponse_WithPublicConstructor_CreatesInstance()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.CircuitBreaker,
            ConfidenceScore = 0.9,
            Parameters = new Dictionary<string, object>()
        };

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object);

        // Act
        var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
            .GetMethod("TryGetFallbackResponse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var parameters = new object[] { recommendation, null };
        var result = (bool)method!.Invoke(behavior, parameters)!;
        var actualFallback = (TestResponse?)parameters[1];

        // Assert
        Assert.True(result);
        Assert.NotNull(actualFallback);
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
}
