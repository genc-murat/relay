using Relay.Core.AI;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Cache;

public class FifoEvictionPolicyTests
{
    private readonly FifoEvictionPolicy _policy;

    public FifoEvictionPolicyTests()
    {
        _policy = new FifoEvictionPolicy();
    }

    [Fact]
    public void OnAdd_DoesNotTrackState()
    {
        // Act
        _policy.OnAdd("key1");

        // Assert - FIFO doesn't maintain internal state, so GetKeyToEvict depends only on cache
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(createdAt: DateTime.UtcNow.AddMinutes(-10))
        };
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void OnAccess_DoesNothing()
    {
        // Arrange
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(createdAt: DateTime.UtcNow.AddMinutes(-10)),
            ["key2"] = CreateTestEntry(createdAt: DateTime.UtcNow.AddMinutes(-5))
        };

        // Act - Access should not affect FIFO
        _policy.OnAccess("key1");

        // Assert - Still evicts oldest (key1)
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void OnRemove_DoesNothing()
    {
        // Arrange
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(createdAt: DateTime.UtcNow.AddMinutes(-10)),
            ["key2"] = CreateTestEntry(createdAt: DateTime.UtcNow.AddMinutes(-5))
        };

        // Act - Remove should not affect FIFO since it doesn't track state
        _policy.OnRemove("key1");

        // Assert - Still evicts oldest remaining (key1, but since it's removed from cache, key2)
        cache.Remove("key1");
        Assert.Equal("key2", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void GetKeyToEvict_ReturnsNull_WhenCacheIsEmpty()
    {
        // Act
        var result = _policy.GetKeyToEvict(new Dictionary<string, CacheEntry>());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetKeyToEvict_ReturnsOldestEntry()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(createdAt: now.AddMinutes(-10)),
            ["key2"] = CreateTestEntry(createdAt: now.AddMinutes(-5)),
            ["key3"] = CreateTestEntry(createdAt: now.AddMinutes(-15))
        };

        // Act
        var result = _policy.GetKeyToEvict(cache);

        // Assert - key3 is oldest
        Assert.Equal("key3", result);
    }

    [Fact]
    public void GetKeyToEvict_HandlesSameCreationTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(createdAt: now),
            ["key2"] = CreateTestEntry(createdAt: now),
            ["key3"] = CreateTestEntry(createdAt: now)
        };

        // Act
        var result = _policy.GetKeyToEvict(cache);

        // Assert - When creation times are equal, returns first in dictionary order (implementation detail)
        Assert.NotNull(result);
        Assert.Contains(result, new[] { "key1", "key2", "key3" });
    }

    [Fact]
    public void GetKeyToEvict_IgnoresPolicyState()
    {
        // Arrange - Add some keys to policy (should be ignored)
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");
        _policy.OnAccess("key1");

        var now = DateTime.UtcNow;
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key3"] = CreateTestEntry(createdAt: now.AddMinutes(-10)), // oldest
            ["key4"] = CreateTestEntry(createdAt: now.AddMinutes(-5))
        };

        // Act
        var result = _policy.GetKeyToEvict(cache);

        // Assert - Ignores policy state, evicts oldest in cache
        Assert.Equal("key3", result);
    }

    private static CacheEntry CreateTestEntry(DateTime? createdAt = null)
    {
        return new CacheEntry
        {
            Recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                Priority = OptimizationPriority.Medium,
                Reasoning = "Test"
            },
            ExpiresAt = DateTime.Now.AddHours(1),
            CreatedAt = createdAt ?? DateTime.Now,
            AccessCount = 1,
            LastAccessedAt = DateTime.Now
        };
    }
}