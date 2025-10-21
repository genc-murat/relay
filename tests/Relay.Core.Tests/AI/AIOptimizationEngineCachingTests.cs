using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AIOptimizationEngineCachingTests : IDisposable
{
    private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
    private readonly AIOptimizationOptions _options;
    private readonly AIOptimizationEngine _engine;

    public AIOptimizationEngineCachingTests()
    {
        _loggerMock = new Mock<ILogger<AIOptimizationEngine>>();
        _options = new AIOptimizationOptions
        {
            DefaultBatchSize = 10,
            MaxBatchSize = 100,
            ModelUpdateInterval = TimeSpan.FromMinutes(5),
            ModelTrainingDate = DateTime.UtcNow,
            ModelVersion = "1.0.0",
            LastRetrainingDate = DateTime.UtcNow.AddDays(-1)
        };

        var optionsMock = new Mock<IOptions<AIOptimizationOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        _engine = new AIOptimizationEngine(_loggerMock.Object, optionsMock.Object);
    }

    public void Dispose()
    {
        _engine?.Dispose();
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Throw_When_Disposed()
    {
        // Arrange
        _engine.Dispose();
        var accessPatterns = new AccessPattern[0];

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns));
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Return_Caching_Recommendation()
    {
        // Arrange
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessCount = 10,
                TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                Timestamp = DateTime.UtcNow,
                RequestKey = "test",
                WasCacheHit = true,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessFrequency = 2.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                DataVolatility = 0.1,
                SampleSize = 10
            }
        };

        // Act
        var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ExpectedHitRate >= 0 && recommendation.ExpectedHitRate <= 1);
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Recommend_Caching_For_High_Repeat_Rate()
    {
        // Arrange - Multiple access patterns with repeated keys to create high repeat rate
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessCount = 5,
                TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                Timestamp = DateTime.UtcNow,
                RequestKey = "frequent",
                WasCacheHit = true,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessFrequency = 2.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                DataVolatility = 0.1,
                SampleSize = 5
            },
            new AccessPattern
            {
                AccessCount = 3,
                TimeSinceLastAccess = TimeSpan.FromMinutes(3),
                Timestamp = DateTime.UtcNow.AddMinutes(1),
                RequestKey = "frequent", // Same key - repeat
                WasCacheHit = true,
                ExecutionTime = TimeSpan.FromMilliseconds(95),
                AccessFrequency = 2.5,
                AverageExecutionTime = TimeSpan.FromMilliseconds(95),
                DataVolatility = 0.1,
                SampleSize = 3
            },
            new AccessPattern
            {
                AccessCount = 2,
                TimeSinceLastAccess = TimeSpan.FromMinutes(10),
                Timestamp = DateTime.UtcNow.AddMinutes(2),
                RequestKey = "rare",
                WasCacheHit = false,
                ExecutionTime = TimeSpan.FromMilliseconds(150),
                AccessFrequency = 0.2,
                AverageExecutionTime = TimeSpan.FromMilliseconds(150),
                DataVolatility = 0.3,
                SampleSize = 2
            }
        };

        // Act
        var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ShouldCache, "Should recommend caching for high repeat rate");
        Assert.True(recommendation.ExpectedHitRate > 0.5, "Expected hit rate should be high");
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Not_Recommend_Caching_For_Low_Repeat_Rate()
    {
        // Arrange - All unique keys, no repeats
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessCount = 1,
                TimeSinceLastAccess = TimeSpan.FromHours(1),
                Timestamp = DateTime.UtcNow,
                RequestKey = "rare1",
                WasCacheHit = false,
                ExecutionTime = TimeSpan.FromMilliseconds(50),
                AccessFrequency = 0.1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                DataVolatility = 0.9,
                SampleSize = 1
            },
            new AccessPattern
            {
                AccessCount = 1,
                TimeSinceLastAccess = TimeSpan.FromHours(2),
                Timestamp = DateTime.UtcNow.AddMinutes(1),
                RequestKey = "rare2",
                WasCacheHit = false,
                ExecutionTime = TimeSpan.FromMilliseconds(60),
                AccessFrequency = 0.05,
                AverageExecutionTime = TimeSpan.FromMilliseconds(60),
                DataVolatility = 0.8,
                SampleSize = 1
            },
            new AccessPattern
            {
                AccessCount = 1,
                TimeSinceLastAccess = TimeSpan.FromHours(3),
                Timestamp = DateTime.UtcNow.AddMinutes(2),
                RequestKey = "rare3",
                WasCacheHit = false,
                ExecutionTime = TimeSpan.FromMilliseconds(55),
                AccessFrequency = 0.03,
                AverageExecutionTime = TimeSpan.FromMilliseconds(55),
                DataVolatility = 0.7,
                SampleSize = 1
            }
        };

        // Act
        var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

        // Assert
        Assert.NotNull(recommendation);
        Assert.False(recommendation.ShouldCache, "Should not recommend caching for low repeat rate");
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Handle_Empty_Access_Patterns()
    {
        // Arrange
        var accessPatterns = Array.Empty<AccessPattern>();

        // Act
        var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

        // Assert
        Assert.NotNull(recommendation);
        Assert.False(recommendation.ShouldCache, "Should not recommend caching with no access patterns");
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Handle_Multiple_Access_Patterns()
    {
        // Arrange
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessCount = 20,
                TimeSinceLastAccess = TimeSpan.FromMinutes(10),
                Timestamp = DateTime.UtcNow,
                RequestKey = "pattern1",
                WasCacheHit = true,
                ExecutionTime = TimeSpan.FromMilliseconds(80),
                AccessFrequency = 5.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(80),
                DataVolatility = 0.2,
                SampleSize = 20
            },
            new AccessPattern
            {
                AccessCount = 15,
                TimeSinceLastAccess = TimeSpan.FromMinutes(15),
                Timestamp = DateTime.UtcNow,
                RequestKey = "pattern2",
                WasCacheHit = false,
                ExecutionTime = TimeSpan.FromMilliseconds(120),
                AccessFrequency = 3.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(120),
                DataVolatility = 0.3,
                SampleSize = 15
            }
        };

        // Act
        var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ExpectedHitRate >= 0 && recommendation.ExpectedHitRate <= 1);
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Consider_Data_Volatility()
    {
        // Arrange - High volatility should reduce caching recommendation
        var volatilePatterns = new[]
        {
            new AccessPattern
            {
                AccessCount = 30,
                TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                Timestamp = DateTime.UtcNow,
                RequestKey = "volatile",
                WasCacheHit = true,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessFrequency = 8.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                DataVolatility = 0.9, // High volatility
                SampleSize = 30
            }
        };

        var stablePatterns = new[]
        {
            new AccessPattern
            {
                AccessCount = 30,
                TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                Timestamp = DateTime.UtcNow,
                RequestKey = "stable",
                WasCacheHit = true,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessFrequency = 8.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                DataVolatility = 0.1, // Low volatility
                SampleSize = 30
            }
        };

        // Act
        var volatileRecommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), volatilePatterns);
        var stableRecommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), stablePatterns);

        // Assert
        Assert.NotNull(volatileRecommendation);
        Assert.NotNull(stableRecommendation);
        // Stable data should generally have higher or equal hit rate expectation
        Assert.True(stableRecommendation.ExpectedHitRate >= volatileRecommendation.ExpectedHitRate * 0.8,
            "Stable data should have similar or better caching prospects");
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Handle_Very_Large_Access_Count()
    {
        // Arrange
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessCount = int.MaxValue,
                TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                Timestamp = DateTime.UtcNow,
                RequestKey = "popular",
                WasCacheHit = true,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessFrequency = 1000.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                DataVolatility = 0.1,
                SampleSize = int.MaxValue
            }
        };

        // Act
        var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ExpectedHitRate >= 0 && recommendation.ExpectedHitRate <= 1);
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Handle_Null_RequestType()
    {
        // Arrange
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessCount = 10,
                TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                Timestamp = DateTime.UtcNow,
                RequestKey = "test",
                WasCacheHit = true,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                AccessFrequency = 2.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                DataVolatility = 0.1,
                SampleSize = 10
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.ShouldCacheAsync(null!, accessPatterns));
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Handle_Null_AccessPatterns()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.ShouldCacheAsync(typeof(TestRequest), null!));
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Handle_Mixed_Cache_Hit_Patterns()
    {
        // Arrange - Mix of hits and misses
        var accessPatterns = new[]
        {
            new AccessPattern
            {
                AccessCount = 10,
                TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                Timestamp = DateTime.UtcNow,
                RequestKey = "key1",
                WasCacheHit = true, // Hit
                ExecutionTime = TimeSpan.FromMilliseconds(10),
                AccessFrequency = 5.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(10),
                DataVolatility = 0.1,
                SampleSize = 10
            },
            new AccessPattern
            {
                AccessCount = 10,
                TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                Timestamp = DateTime.UtcNow,
                RequestKey = "key2",
                WasCacheHit = false, // Miss
                ExecutionTime = TimeSpan.FromMilliseconds(200),
                AccessFrequency = 5.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(200),
                DataVolatility = 0.1,
                SampleSize = 10
            },
            new AccessPattern
            {
                AccessCount = 10,
                TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                Timestamp = DateTime.UtcNow,
                RequestKey = "key3",
                WasCacheHit = true, // Hit
                ExecutionTime = TimeSpan.FromMilliseconds(10),
                AccessFrequency = 5.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(10),
                DataVolatility = 0.1,
                SampleSize = 10
            }
        };

        // Act
        var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ExpectedHitRate >= 0 && recommendation.ExpectedHitRate <= 1);
    }

    #region AnalyzeCachingPatterns Tests

    [Fact]
    public void AnalyzeCachingPatterns_WithHighRepeatRate_ShouldRecommendCaching()
    {
        // Arrange
        var context = CreatePatternAnalysisContext(repeatRate: 0.8); // 80% repeat rate

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldCache);
        Assert.True(result.ExpectedHitRate > 0.8); // Should be high
        Assert.True(result.ExpectedImprovement > 0.6); // Should be significant
        Assert.True(result.Confidence > 0.8); // Should be confident
        Assert.Contains("High repeat rate", result.Reasoning);
        Assert.Equal(CacheStrategy.LFU, result.RecommendedStrategy); // High repeat rate = LFU
        Assert.True(result.RecommendedTTL > TimeSpan.Zero);
    }

    [Fact]
    public void AnalyzeCachingPatterns_WithMediumRepeatRate_ShouldRecommendCachingWithLRU()
    {
        // Arrange
        var context = CreatePatternAnalysisContext(repeatRate: 0.4); // 40% repeat rate

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldCache);
        Assert.True(result.ExpectedHitRate > 0.4);
        Assert.True(result.ExpectedImprovement > 0.3);
        Assert.True(result.Confidence > 0.5);
        Assert.Contains("High repeat rate", result.Reasoning);
        Assert.Equal(CacheStrategy.LRU, result.RecommendedStrategy); // Medium repeat rate = LRU
        Assert.True(result.RecommendedTTL > TimeSpan.Zero);
    }

    [Fact]
    public void AnalyzeCachingPatterns_WithLowRepeatRate_ShouldNotRecommendCaching()
    {
        // Arrange
        var context = CreatePatternAnalysisContext(repeatRate: 0.1); // 10% repeat rate

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(result.ShouldCache);
        Assert.Equal(0.0, result.ExpectedHitRate);
        Assert.Equal(0.0, result.ExpectedImprovement);
        Assert.Equal(0.0, result.Confidence);
        Assert.Equal(string.Empty, result.Reasoning);
        Assert.Equal(CacheStrategy.None, result.RecommendedStrategy);
        Assert.Equal(TimeSpan.Zero, result.RecommendedTTL);
    }

    [Fact]
    public void AnalyzeCachingPatterns_WithZeroExecutions_ShouldNotRecommendCaching()
    {
        // Arrange
        var context = CreatePatternAnalysisContext(totalExecutions: 0, repeatRate: 0.5);

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(result.ShouldCache);
        Assert.Equal(0.0, result.ExpectedHitRate);
        Assert.Equal(0.0, result.ExpectedImprovement);
        Assert.Equal(0.0, result.Confidence);
        Assert.Equal(string.Empty, result.Reasoning);
        Assert.Equal(CacheStrategy.None, result.RecommendedStrategy);
        Assert.Equal(TimeSpan.Zero, result.RecommendedTTL);
    }

    [Fact]
    public void AnalyzeCachingPatterns_WithVeryHighRepeatRate_ShouldCapHitRate()
    {
        // Arrange
        var context = CreatePatternAnalysisContext(repeatRate: 0.9); // 90% repeat rate

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldCache);
        Assert.True(result.ExpectedHitRate <= 0.95); // Should be capped at 95%
        Assert.True(result.ExpectedHitRate > 0.8);
    }

    [Fact]
    public void AnalyzeCachingPatterns_ShouldCalculateConfidenceBasedOnRepeatRate()
    {
        // Arrange
        var context = CreatePatternAnalysisContext(repeatRate: 0.3); // 30% repeat rate

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldCache);
        // Confidence should be scaled: 0.5 + (0.3 * 1.5) = 0.95, but capped at 0.9
        Assert.Equal(0.9, result.Confidence);
    }

    [Fact]
    public void AnalyzeCachingPatterns_ShouldCalculateTTLBasedOnRepeatRate()
    {
        // Arrange
        var context = CreatePatternAnalysisContext(repeatRate: 1.0); // 100% repeat rate

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldCache);
        // TTL should be calculated based on repeat rate (higher repeat rate = shorter TTL)
        // For repeatRate=1.0, avgInterval=30 minutes, so TTL=30 minutes
        Assert.Equal(TimeSpan.FromMinutes(30), result.RecommendedTTL);
    }

    [Fact]
    public void AnalyzeCachingPatterns_ShouldRespectMinMaxTTLConstraints()
    {
        // Arrange - Create context with very low repeat rate to get long TTL
        var context = CreatePatternAnalysisContext(repeatRate: 0.95); // 95% repeat rate

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldCache);
        // TTL should be constrained by min/max values from options
        Assert.True(result.RecommendedTTL >= _options.MinCacheTtl);
        Assert.True(result.RecommendedTTL <= _options.MaxCacheTtl);
    }

    [Fact]
    public void AnalyzeCachingPatterns_ShouldIncludeRepeatRateInReasoning()
    {
        // Arrange
        var context = CreatePatternAnalysisContext(repeatRate: 0.75); // 75% repeat rate

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldCache);
        Assert.Contains("75.00%", result.Reasoning);
        Assert.Contains("improvement", result.Reasoning);
    }

    [Theory]
    [InlineData(0.25, CacheStrategy.LRU)]
    [InlineData(0.55, CacheStrategy.LRU)]
    [InlineData(0.65, CacheStrategy.LFU)]
    [InlineData(0.85, CacheStrategy.LFU)]
    public void AnalyzeCachingPatterns_ShouldSelectCorrectStrategy(double repeatRate, CacheStrategy expectedStrategy)
    {
        // Arrange
        var context = CreatePatternAnalysisContext(repeatRate: repeatRate);

        // Act
        var result = _engine.GetType().GetMethod("AnalyzeCachingPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { typeof(PatternAnalysisContext) }, null)?
            .Invoke(_engine, new object[] { context }) as CachingAnalysisResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldCache);
        Assert.Equal(expectedStrategy, result.RecommendedStrategy);
    }

    private PatternAnalysisContext CreatePatternAnalysisContext(double repeatRate = 0.0, int totalExecutions = 100)
    {
        var analysisData = new RequestAnalysisData();
        var metrics = CreateMetrics(totalExecutions);

        // Add metrics to populate the analysis data
        analysisData.AddMetrics(metrics);

        // Manually set repeat request count based on repeat rate using reflection
        var repeatCount = (int)(totalExecutions * repeatRate);
        var repeatProperty = typeof(RequestAnalysisData).GetProperty("RepeatRequestCount",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (repeatProperty != null)
        {
            // Use reflection to set the private setter
            repeatProperty.SetValue(analysisData, repeatCount);
        }

        return new PatternAnalysisContext
        {
            AnalysisData = analysisData,
            CurrentMetrics = metrics,
            SystemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.6,
                AvailableMemory = 1024 * 1024 * 1024, // 1GB
                ActiveRequestCount = 10,
                QueuedRequestCount = 5,
                ThroughputPerSecond = 100.0,
                AverageResponseTime = TimeSpan.FromMilliseconds(50),
                ErrorRate = 0.02,
                Timestamp = DateTime.UtcNow,
                ActiveConnections = 50,
                DatabasePoolUtilization = 0.3,
                ThreadPoolUtilization = 0.4
            },
            HistoricalTrend = 0.1,
            RequestType = typeof(TestRequest)
        };
    }

    private RequestExecutionMetrics CreateMetrics(int executionCount = 100, TimeSpan? averageExecutionTime = null, int databaseCalls = 2, int externalApiCalls = 1, int failedExecutions = -1)
    {
        var avgTime = averageExecutionTime ?? TimeSpan.FromMilliseconds(100);
        var failed = failedExecutions >= 0 ? failedExecutions : executionCount / 10; // Default 10% failure rate
        return new RequestExecutionMetrics
        {
            AverageExecutionTime = avgTime,
            MedianExecutionTime = avgTime - TimeSpan.FromMilliseconds(5),
            P95ExecutionTime = avgTime + TimeSpan.FromMilliseconds(50),
            P99ExecutionTime = avgTime + TimeSpan.FromMilliseconds(100),
            TotalExecutions = executionCount,
            SuccessfulExecutions = executionCount - failed,
            FailedExecutions = failed,
            MemoryAllocated = 1024 * 1024,
            ConcurrentExecutions = 10,
            LastExecution = DateTime.UtcNow,
            SamplePeriod = TimeSpan.FromMinutes(5),
            CpuUsage = 0.45,
            MemoryUsage = 512 * 1024,
            DatabaseCalls = databaseCalls,
            ExternalApiCalls = externalApiCalls
        };
    }

    #endregion

    #region Test Types

    private class TestRequest { }

    #endregion
}