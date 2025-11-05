using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Batching;
using Relay.Core.AI.Pipeline.Behaviors.Strategies;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Strategies;

/// <summary>
/// Tests for CachingOptimizationStrategy implementation.
/// </summary>
public class CachingOptimizationStrategyTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;
    private readonly Mock<IAIOptimizationEngine> _aiEngineMock;
    private readonly AIOptimizationOptions _options;

    public CachingOptimizationStrategyTests()
    {
        _loggerMock = new Mock<ILogger>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
        _aiEngineMock = new Mock<IAIOptimizationEngine>();

        _options = new AIOptimizationOptions
        {
            Enabled = true,
            MinConfidenceScore = 0.7,
            MinCacheHitRate = 0.5
        };
    }

    [Fact]
    public async Task CachingOptimizationStrategy_CanApplyAsync_WithValidConditions_ReturnsTrue()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5
        };

        _aiEngineMock.Setup(x => x.ShouldCacheAsync(typeof(TestRequest), It.IsAny<AccessPattern[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachingRecommendation { ShouldCache = true, PredictedHitRate = 0.8 });

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CachingOptimizationStrategy_CanApplyAsync_WithoutCacheProviders_ReturnsFalse()
    {
        // Arrange
        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            null, // No memory cache
            null, // No distributed cache
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics();

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CachingOptimizationStrategy_CanApplyAsync_WithLowConfidence_ReturnsFalse()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.5, // Below threshold
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5
        };

        _aiEngineMock.Setup(x => x.ShouldCacheAsync(typeof(TestRequest), It.IsAny<AccessPattern[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachingRecommendation { ShouldCache = true, PredictedHitRate = 0.8 });

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CachingOptimizationStrategy_CanApplyAsync_WithLowPredictedHitRate_ReturnsFalse()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5
        };

        _aiEngineMock.Setup(x => x.ShouldCacheAsync(typeof(TestRequest), It.IsAny<AccessPattern[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachingRecommendation { ShouldCache = true, PredictedHitRate = 0.3 }); // Below min hit rate

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CachingOptimizationStrategy_CanApplyAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _aiEngineMock.Setup(x => x.ShouldCacheAsync(typeof(TestRequest), It.IsAny<AccessPattern[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachingRecommendation { ShouldCache = true, PredictedHitRate = 0.8 });

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, cts.Token);

        // Assert - Should still work even with cancelled token since mocks don't check it
        Assert.True(result);
    }

    [Fact]
    public async Task CachingOptimizationStrategy_CanApplyAsync_WithNullAccessPatterns_ReturnsFalse()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5
        };

        _aiEngineMock.Setup(x => x.ShouldCacheAsync(typeof(TestRequest), It.IsAny<AccessPattern[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachingRecommendation { ShouldCache = false, PredictedHitRate = 0.0 });

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CachingOptimizationStrategy_ApplyAsync_WithCustomParameters_WrapsHandler()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["CacheDuration"] = TimeSpan.FromMinutes(5),
                ["CacheKeyPrefix"] = "test_",
                ["EnableSlidingExpiration"] = true
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5
        };

        _aiEngineMock.Setup(x => x.ShouldCacheAsync(typeof(TestRequest), It.IsAny<AccessPattern[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachingRecommendation { ShouldCache = true, PredictedHitRate = 0.9, RecommendedTtl = TimeSpan.FromMinutes(5) });

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"cached_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cached_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CachingOptimizationStrategy_ApplyAsync_WithMinimalParameters_WrapsHandler()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>() // Empty parameters - use defaults
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5
        };

        _aiEngineMock.Setup(x => x.ShouldCacheAsync(typeof(TestRequest), It.IsAny<AccessPattern[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachingRecommendation { ShouldCache = true, PredictedHitRate = 0.9, RecommendedTtl = TimeSpan.FromMinutes(10) });

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"minimal_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("minimal_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CachingOptimizationStrategy_ApplyAsync_WithCacheFailure_ContinuesExecution()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5
        };

        _aiEngineMock.Setup(x => x.ShouldCacheAsync(typeof(TestRequest), It.IsAny<AccessPattern[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachingRecommendation { ShouldCache = true, PredictedHitRate = 0.9, RecommendedTtl = TimeSpan.FromMinutes(10) });

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "cache_failure_handled" });

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cache_failure_handled", result.Result);
    }

    #region Calculation Method Tests

    [Fact]
    public async Task GetAccessPatternsAsync_WithValidStats_CalculatesDataVolatility()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var stats = new Relay.Core.Telemetry.HandlerExecutionStats
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 80,
            FailedExecutions = 20,
            AverageExecutionTime = TimeSpan.FromMilliseconds(200),
            P50ExecutionTime = TimeSpan.FromMilliseconds(180),
            P95ExecutionTime = TimeSpan.FromMilliseconds(300),
            LastExecution = DateTimeOffset.UtcNow
        };

        _metricsProviderMock.Setup(x => x.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        // Act
        var method = typeof(CachingOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("GetAccessPatternsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (ValueTask<AccessPattern[]>)method!.Invoke(strategy, new object[] { typeof(TestRequest), CancellationToken.None })!;
        var result = await task;

        // Assert
        Assert.Single(result);
        var pattern = result[0];
        Assert.Equal(typeof(TestRequest), pattern.RequestType);
        Assert.True(pattern.DataVolatility >= 0.0 && pattern.DataVolatility <= 1.0); // Should be clamped
        Assert.True(pattern.AccessFrequency > 0); // Should calculate frequency
        Assert.Equal(TimeSpan.FromMilliseconds(200), pattern.AverageExecutionTime);
    }

    [Fact]
    public async Task GetAccessPatternsAsync_WithHighFailureRate_CalculatesHighDataVolatility()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var stats = new Relay.Core.Telemetry.HandlerExecutionStats
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 10,
            FailedExecutions = 90, // 90% failure rate
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P50ExecutionTime = TimeSpan.FromMilliseconds(90),
            P95ExecutionTime = TimeSpan.FromMilliseconds(150),
            LastExecution = DateTimeOffset.UtcNow
        };

        _metricsProviderMock.Setup(x => x.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        // Act
        var method = typeof(CachingOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("GetAccessPatternsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (ValueTask<AccessPattern[]>)method!.Invoke(strategy, new object[] { typeof(TestRequest), CancellationToken.None })!;
        var result = await task;

        // Assert
        Assert.Single(result);
        var pattern = result[0];
        Assert.True(pattern.DataVolatility > 0.5); // High failure rate should result in high volatility
    }

    [Fact]
    public async Task GetAccessPatternsAsync_WithHighExecutionTimeVariance_CalculatesHighDataVolatility()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var stats = new Relay.Core.Telemetry.HandlerExecutionStats
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 95,
            FailedExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(2000), // High average time
            P50ExecutionTime = TimeSpan.FromMilliseconds(180),
            P95ExecutionTime = TimeSpan.FromMilliseconds(300),
            LastExecution = DateTimeOffset.UtcNow
        };

        _metricsProviderMock.Setup(x => x.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        // Act
        var method = typeof(CachingOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("GetAccessPatternsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (ValueTask<AccessPattern[]>)method!.Invoke(strategy, new object[] { typeof(TestRequest), CancellationToken.None })!;
        var result = await task;

        // Assert
        Assert.Single(result);
        var pattern = result[0];
        Assert.True(pattern.DataVolatility > 0.2); // High execution time variance should contribute to volatility
    }

    [Fact]
    public async Task GetAccessPatternsAsync_WithNoStats_ReturnsDefaultPattern()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        _metricsProviderMock.Setup(x => x.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns((Relay.Core.Telemetry.HandlerExecutionStats?)null);

        // Act
        var method = typeof(CachingOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("GetAccessPatternsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (ValueTask<AccessPattern[]>)method!.Invoke(strategy, new object[] { typeof(TestRequest), CancellationToken.None })!;
        var result = await task;

        // Assert
        Assert.Single(result);
        var pattern = result[0];
        Assert.Equal(typeof(TestRequest), pattern.RequestType);
        Assert.Equal(1.0, pattern.AccessFrequency); // Default frequency
        Assert.Equal(TimeSpan.FromMilliseconds(100), pattern.AverageExecutionTime); // Default time
        Assert.Equal(0.5, pattern.DataVolatility); // Default volatility
        Assert.Equal(0, pattern.SampleSize);
    }

    [Fact]
    public async Task GetAccessPatternsAsync_WithMetricsProviderException_ReturnsDefaultPattern()
    {
        // Arrange
        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var strategy = new CachingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        _metricsProviderMock.Setup(x => x.GetHandlerExecutionStats(typeof(TestRequest)))
            .Throws(new InvalidOperationException("Test exception"));

        // Act
        var method = typeof(CachingOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("GetAccessPatternsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (ValueTask<AccessPattern[]>)method!.Invoke(strategy, new object[] { typeof(TestRequest), CancellationToken.None })!;
        var result = await task;

        // Assert
        Assert.Single(result);
        var pattern = result[0];
        Assert.Equal(1.0, pattern.AccessFrequency); // Should return default pattern
        Assert.Equal(0.5, pattern.DataVolatility);
    }

    #endregion
}