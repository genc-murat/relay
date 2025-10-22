using Relay.Core.AI;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.Cache;

public class CacheEntryTests
{
    private readonly DateTime _testTime = new DateTime(2023, 1, 1, 12, 0, 0);
    private readonly OptimizationRecommendation _testRecommendation;

    public CacheEntryTests()
    {
        _testRecommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            Priority = OptimizationPriority.High,
            Reasoning = "Test recommendation"
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesEntry()
    {
        // Arrange
        var expiresAt = _testTime.AddHours(1);
        var createdAt = _testTime;

        // Act
        var entry = new CacheEntry
        {
            Recommendation = _testRecommendation,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            AccessCount = 0,
            LastAccessedAt = createdAt
        };

        // Assert
        Assert.Equal(_testRecommendation, entry.Recommendation);
        Assert.Equal(expiresAt, entry.ExpiresAt);
        Assert.Equal(createdAt, entry.CreatedAt);
        Assert.Equal(0, entry.AccessCount);
        Assert.Equal(createdAt, entry.LastAccessedAt);
    }

    [Fact]
    public void Properties_AreInitializedWithDefaults_WhenNotSet()
    {
        // Act
        var entry = new CacheEntry();

        // Assert
        Assert.Null(entry.Recommendation);
        Assert.Equal(default(DateTime), entry.ExpiresAt);
        Assert.Equal(default(DateTime), entry.CreatedAt);
        Assert.Equal(0, entry.AccessCount);
        Assert.Equal(default(DateTime), entry.LastAccessedAt);
    }

    [Fact]
    public void AccessCount_CanBeIncremented()
    {
        // Arrange
        var entry = new CacheEntry
        {
            Recommendation = _testRecommendation,
            AccessCount = 5
        };

        // Act
        entry.AccessCount++;

        // Assert
        Assert.Equal(6, entry.AccessCount);
    }

    [Fact]
    public void LastAccessedAt_CanBeUpdated()
    {
        // Arrange
        var entry = new CacheEntry
        {
            Recommendation = _testRecommendation,
            LastAccessedAt = _testTime
        };
        var newAccessTime = _testTime.AddMinutes(30);

        // Act
        entry.LastAccessedAt = newAccessTime;

        // Assert
        Assert.Equal(newAccessTime, entry.LastAccessedAt);
    }

    [Fact]
    public void ExpiresAt_IsInitializedCorrectly()
    {
        // Arrange
        var newExpiry = _testTime.AddDays(1);

        // Act
        var entry = new CacheEntry
        {
            Recommendation = _testRecommendation,
            ExpiresAt = newExpiry,
            CreatedAt = _testTime,
            AccessCount = 0,
            LastAccessedAt = _testTime
        };

        // Assert
        Assert.Equal(newExpiry, entry.ExpiresAt);
    }

    [Fact]
    public void CreatedAt_IsInitializedCorrectly()
    {
        // Arrange
        var newCreatedAt = _testTime.AddSeconds(1);

        // Act
        var entry = new CacheEntry
        {
            Recommendation = _testRecommendation,
            ExpiresAt = _testTime.AddHours(1),
            CreatedAt = newCreatedAt,
            AccessCount = 0,
            LastAccessedAt = _testTime
        };

        // Assert
        Assert.Equal(newCreatedAt, entry.CreatedAt);
    }

    [Fact]
    public void Recommendation_IsInitializedCorrectly()
    {
        // Arrange
        var newRecommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.DatabaseOptimization,
            Priority = OptimizationPriority.Medium,
            Reasoning = "New recommendation"
        };

        // Act
        var entry = new CacheEntry
        {
            Recommendation = newRecommendation,
            ExpiresAt = _testTime.AddHours(1),
            CreatedAt = _testTime,
            AccessCount = 0,
            LastAccessedAt = _testTime
        };

        // Assert
        Assert.Equal(newRecommendation, entry.Recommendation);
        Assert.Equal(OptimizationStrategy.DatabaseOptimization, entry.Recommendation.Strategy);
    }

    [Fact]
    public void Entry_CanBeUsedInCollections()
    {
        // Arrange
        var entries = new System.Collections.Generic.List<CacheEntry>();
        var entry1 = new CacheEntry
        {
            Recommendation = _testRecommendation,
            AccessCount = 10
        };
        var entry2 = new CacheEntry
        {
            Recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.MemoryOptimization,
                Priority = OptimizationPriority.Low,
                Reasoning = "Another recommendation"
            },
            AccessCount = 5
        };

        // Act
        entries.Add(entry1);
        entries.Add(entry2);

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.AccessCount == 10);
        Assert.Contains(entries, e => e.AccessCount == 5);
    }

    [Fact]
    public void Entry_PropertiesAreIndependent()
    {
        // Arrange
        var entry1 = new CacheEntry
        {
            Recommendation = _testRecommendation,
            AccessCount = 1,
            LastAccessedAt = _testTime
        };
        var entry2 = new CacheEntry
        {
            Recommendation = _testRecommendation,
            AccessCount = 2,
            LastAccessedAt = _testTime.AddHours(1)
        };

        // Act
        entry1.AccessCount = 100;

        // Assert
        Assert.Equal(100, entry1.AccessCount);
        Assert.Equal(2, entry2.AccessCount); // Should remain unchanged
        Assert.Equal(_testTime, entry1.LastAccessedAt);
        Assert.Equal(_testTime.AddHours(1), entry2.LastAccessedAt);
    }
}