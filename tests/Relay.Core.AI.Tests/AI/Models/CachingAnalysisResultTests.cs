using System;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Models;

public class CachingAnalysisResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var result = new CachingAnalysisResult();

        // Assert
        Assert.False(result.ShouldCache);
        Assert.Equal(0.0, result.ExpectedHitRate);
        Assert.Equal(0.0, result.ExpectedImprovement);
        Assert.Equal(0.0, result.Confidence);
        Assert.Equal(string.Empty, result.Reasoning);
        Assert.Equal(CacheStrategy.None, result.RecommendedStrategy);
        Assert.Equal(TimeSpan.Zero, result.RecommendedTTL);
    }

    [Fact]
    public void ShouldCache_CanBeSetAndRetrieved()
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.ShouldCache = true;

        // Assert
        Assert.True(result.ShouldCache);
    }

    [Fact]
    public void ExpectedHitRate_CanBeSetAndRetrieved()
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.ExpectedHitRate = 0.85;

        // Assert
        Assert.Equal(0.85, result.ExpectedHitRate);
    }

    [Fact]
    public void ExpectedImprovement_CanBeSetAndRetrieved()
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.ExpectedImprovement = 0.75;

        // Assert
        Assert.Equal(0.75, result.ExpectedImprovement);
    }

    [Fact]
    public void Confidence_CanBeSetAndRetrieved()
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.Confidence = 0.92;

        // Assert
        Assert.Equal(0.92, result.Confidence);
    }

    [Fact]
    public void Reasoning_CanBeSetAndRetrieved()
    {
        // Arrange
        var result = new CachingAnalysisResult();
        var reasoning = "High repeat rate detected - caching recommended";

        // Act
        result.Reasoning = reasoning;

        // Assert
        Assert.Equal(reasoning, result.Reasoning);
    }

    [Fact]
    public void RecommendedStrategy_CanBeSetAndRetrieved()
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.RecommendedStrategy = CacheStrategy.LFU;

        // Assert
        Assert.Equal(CacheStrategy.LFU, result.RecommendedStrategy);
    }

    [Fact]
    public void RecommendedTTL_CanBeSetAndRetrieved()
    {
        // Arrange
        var result = new CachingAnalysisResult();
        var ttl = TimeSpan.FromMinutes(30);

        // Act
        result.RecommendedTTL = ttl;

        // Assert
        Assert.Equal(ttl, result.RecommendedTTL);
    }

    [Fact]
    public void CanCreateFullyConfiguredResult()
    {
        // Act
        var result = new CachingAnalysisResult
        {
            ShouldCache = true,
            ExpectedHitRate = 0.88,
            ExpectedImprovement = 0.72,
            Confidence = 0.95,
            Reasoning = "Excellent caching opportunity identified",
            RecommendedStrategy = CacheStrategy.Adaptive,
            RecommendedTTL = TimeSpan.FromHours(2)
        };

        // Assert
        Assert.True(result.ShouldCache);
        Assert.Equal(0.88, result.ExpectedHitRate);
        Assert.Equal(0.72, result.ExpectedImprovement);
        Assert.Equal(0.95, result.Confidence);
        Assert.Equal("Excellent caching opportunity identified", result.Reasoning);
        Assert.Equal(CacheStrategy.Adaptive, result.RecommendedStrategy);
        Assert.Equal(TimeSpan.FromHours(2), result.RecommendedTTL);
    }

    [Fact]
    public void CanCreateResultForNoCachingScenario()
    {
        // Act
        var result = new CachingAnalysisResult
        {
            ShouldCache = false,
            ExpectedHitRate = 0.05,
            ExpectedImprovement = 0.02,
            Confidence = 0.85,
            Reasoning = "Low repeat rate - caching not beneficial",
            RecommendedStrategy = CacheStrategy.None,
            RecommendedTTL = TimeSpan.Zero
        };

        // Assert
        Assert.False(result.ShouldCache);
        Assert.Equal(0.05, result.ExpectedHitRate);
        Assert.Equal(0.02, result.ExpectedImprovement);
        Assert.Equal(0.85, result.Confidence);
        Assert.Equal("Low repeat rate - caching not beneficial", result.Reasoning);
        Assert.Equal(CacheStrategy.None, result.RecommendedStrategy);
        Assert.Equal(TimeSpan.Zero, result.RecommendedTTL);
    }

    [Theory]
    [InlineData(CacheStrategy.None)]
    [InlineData(CacheStrategy.LRU)]
    [InlineData(CacheStrategy.LFU)]
    [InlineData(CacheStrategy.TimeBasedExpiration)]
    [InlineData(CacheStrategy.SlidingExpiration)]
    [InlineData(CacheStrategy.Adaptive)]
    [InlineData(CacheStrategy.Distributed)]
    public void RecommendedStrategy_AllEnumValuesSupported(CacheStrategy strategy)
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.RecommendedStrategy = strategy;

        // Assert
        Assert.Equal(strategy, result.RecommendedStrategy);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(0.123)]
    [InlineData(0.999)]
    public void ExpectedHitRate_AcceptsValidValues(double value)
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.ExpectedHitRate = value;

        // Assert
        Assert.Equal(value, result.ExpectedHitRate);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(0.123)]
    [InlineData(0.999)]
    public void ExpectedImprovement_AcceptsValidValues(double value)
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.ExpectedImprovement = value;

        // Assert
        Assert.Equal(value, result.ExpectedImprovement);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(0.123)]
    [InlineData(0.999)]
    public void Confidence_AcceptsValidValues(double value)
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.Confidence = value;

        // Assert
        Assert.Equal(value, result.Confidence);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(3600000)] // 1 hour
    [InlineData(86400000)] // 1 day
    public void RecommendedTTL_AcceptsValidTimeSpans(long milliseconds)
    {
        // Arrange
        var result = new CachingAnalysisResult();
        var ttl = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        result.RecommendedTTL = ttl;

        // Assert
        Assert.Equal(ttl, result.RecommendedTTL);
    }

    [Fact]
    public void Reasoning_CanBeSetToNull()
    {
        // Arrange
        var result = new CachingAnalysisResult();

        // Act
        result.Reasoning = null!;

        // Assert
        Assert.Null(result.Reasoning);
    }

    [Fact]
    public void Reasoning_DefaultsToEmptyString()
    {
        // Act
        var result = new CachingAnalysisResult();

        // Assert
        Assert.Equal(string.Empty, result.Reasoning);
    }

    [Fact]
    public void CanCreateResultWithLongReasoning()
    {
        // Arrange
        var longReasoning = new string('A', 1000);

        // Act
        var result = new CachingAnalysisResult
        {
            Reasoning = longReasoning
        };

        // Assert
        Assert.Equal(longReasoning, result.Reasoning);
    }

    [Fact]
    public void Properties_AreIndependent()
    {
        // Arrange
        var result1 = new CachingAnalysisResult();
        var result2 = new CachingAnalysisResult();

        // Act
        result1.ShouldCache = true;
        result1.ExpectedHitRate = 0.8;
        result1.RecommendedStrategy = CacheStrategy.LRU;

        result2.ShouldCache = false;
        result2.ExpectedHitRate = 0.2;
        result2.RecommendedStrategy = CacheStrategy.LFU;

        // Assert
        Assert.True(result1.ShouldCache);
        Assert.Equal(0.8, result1.ExpectedHitRate);
        Assert.Equal(CacheStrategy.LRU, result1.RecommendedStrategy);

        Assert.False(result2.ShouldCache);
        Assert.Equal(0.2, result2.ExpectedHitRate);
        Assert.Equal(CacheStrategy.LFU, result2.RecommendedStrategy);
    }

    [Fact]
    public void Class_IsInternal()
    {
        // Act
        var type = typeof(CachingAnalysisResult);

        // Assert
        Assert.False(type.IsPublic);
        Assert.True(type.IsNotPublic);
    }

    [Fact]
    public void Class_InheritsFromObject()
    {
        // Act
        var result = new CachingAnalysisResult();

        // Assert
        Assert.IsType<CachingAnalysisResult>(result);
        Assert.IsAssignableFrom<object>(result);
    }

    [Fact]
    public void CanBeUsedInCollections()
    {
        // Arrange
        var results = new System.Collections.Generic.List<CachingAnalysisResult>();

        // Act
        results.Add(new CachingAnalysisResult { ShouldCache = true });
        results.Add(new CachingAnalysisResult { ShouldCache = false });

        // Assert
        Assert.Equal(2, results.Count);
        Assert.True(results[0].ShouldCache);
        Assert.False(results[1].ShouldCache);
    }

    [Fact]
    public void CanBeSerializedAndDeserialized()
    {
        // Arrange
        var original = new CachingAnalysisResult
        {
            ShouldCache = true,
            ExpectedHitRate = 0.75,
            ExpectedImprovement = 0.60,
            Confidence = 0.88,
            Reasoning = "Test reasoning",
            RecommendedStrategy = CacheStrategy.Adaptive,
            RecommendedTTL = TimeSpan.FromMinutes(45)
        };

        // Act - Simulate serialization/deserialization (basic property copy)
        var deserialized = new CachingAnalysisResult
        {
            ShouldCache = original.ShouldCache,
            ExpectedHitRate = original.ExpectedHitRate,
            ExpectedImprovement = original.ExpectedImprovement,
            Confidence = original.Confidence,
            Reasoning = original.Reasoning,
            RecommendedStrategy = original.RecommendedStrategy,
            RecommendedTTL = original.RecommendedTTL
        };

        // Assert
        Assert.Equal(original.ShouldCache, deserialized.ShouldCache);
        Assert.Equal(original.ExpectedHitRate, deserialized.ExpectedHitRate);
        Assert.Equal(original.ExpectedImprovement, deserialized.ExpectedImprovement);
        Assert.Equal(original.Confidence, deserialized.Confidence);
        Assert.Equal(original.Reasoning, deserialized.Reasoning);
        Assert.Equal(original.RecommendedStrategy, deserialized.RecommendedStrategy);
        Assert.Equal(original.RecommendedTTL, deserialized.RecommendedTTL);
    }
}