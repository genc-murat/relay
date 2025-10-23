using Relay.Core.AI;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Cache;

public class LfuEvictionPolicyTests
{
    private readonly LfuEvictionPolicy _policy;

    public LfuEvictionPolicyTests()
    {
        _policy = new LfuEvictionPolicy();
    }

    [Fact]
    public void OnAdd_AddsKeyWithZeroAccessCount()
    {
        // Act
        _policy.OnAdd("key1");

        // Assert - GetKeyToEvict should return the key when cache contains it
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(accessCount: 0)
        };
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void OnAccess_DoesNotAffectEviction()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(accessCount: 0),
            ["key2"] = CreateTestEntry(accessCount: 0)
        };

        // Initially, both have same access count, should evict oldest (key1)
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));

        // Act - Access key1 (LFU policy doesn't track access internally)
        _policy.OnAccess("key1");

        // Assert - Eviction order unchanged since access counts are same
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void OnRemove_RemovesKeyFromConsideration()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(accessCount: 0),
            ["key2"] = CreateTestEntry(accessCount: 0)
        };

        // Initially, key1 should be evicted first
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));

        // Act
        _policy.OnRemove("key1");

        // Assert - Now key2 should be evicted since key1 is no longer tracked
        Assert.Equal("key2", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void GetKeyToEvict_ReturnsNull_WhenCacheIsEmpty()
    {
        // Arrange
        _policy.OnAdd("key1");

        // Act
        var result = _policy.GetKeyToEvict(new Dictionary<string, CacheEntry>());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetKeyToEvict_ReturnsNull_WhenNoKeysInCache()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key3"] = CreateTestEntry(accessCount: 0) // Different key not tracked by policy
        };

        // Act
        var result = _policy.GetKeyToEvict(cache);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetKeyToEvict_ReturnsLeastFrequentlyUsedKey()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");
        _policy.OnAdd("key3");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(accessCount: 5),
            ["key2"] = CreateTestEntry(accessCount: 1),
            ["key3"] = CreateTestEntry(accessCount: 3)
        };

        // Act
        var result = _policy.GetKeyToEvict(cache);

        // Assert - key2 has lowest access count
        Assert.Equal("key2", result);
    }

    [Fact]
    public void GetKeyToEvict_BreaksTiesByLastAccessedTime()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");
        _policy.OnAdd("key3");

        var now = System.DateTime.UtcNow;
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(accessCount: 1, lastAccessed: now.AddMinutes(-10)),
            ["key2"] = CreateTestEntry(accessCount: 1, lastAccessed: now.AddMinutes(-5)),
            ["key3"] = CreateTestEntry(accessCount: 2, lastAccessed: now)
        };

        // Act
        var result = _policy.GetKeyToEvict(cache);

        // Assert - key1 and key2 have same access count, key1 is oldest
        Assert.Equal("key1", result);
    }

    [Fact]
    public void OnAccess_DoesNothing_ForUnknownKey()
    {
        // Arrange
        _policy.OnAdd("key1");
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(accessCount: 0)
        };

        // Act - Access unknown key
        _policy.OnAccess("unknown");

        // Assert - Should not affect eviction order
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void OnRemove_DoesNothing_ForUnknownKey()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(accessCount: 0),
            ["key2"] = CreateTestEntry(accessCount: 0)
        };

        // Act - Remove unknown key
        _policy.OnRemove("unknown");

        // Assert - Should not affect eviction order
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void ComplexAccessPattern_MaintainsCorrectEviction()
    {
        // Arrange
        _policy.OnAdd("a");
        _policy.OnAdd("b");
        _policy.OnAdd("c");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["a"] = CreateTestEntry(accessCount: 0),
            ["b"] = CreateTestEntry(accessCount: 0),
            ["c"] = CreateTestEntry(accessCount: 0)
        };

        // Initially, all have same access count, evict oldest (a)
        Assert.Equal("a", _policy.GetKeyToEvict(cache));

        // Act - Simulate accessing 'a' multiple times (update cache entry)
        cache["a"] = CreateTestEntry(accessCount: 3);

        // Assert - 'a' now has highest access count, evict 'b'
        Assert.Equal("b", _policy.GetKeyToEvict(cache));

        // Act - Simulate accessing 'b' once
        cache["b"] = CreateTestEntry(accessCount: 1);

        // Assert - 'c' has lowest access count
        Assert.Equal("c", _policy.GetKeyToEvict(cache));
    }

    private static CacheEntry CreateTestEntry(int accessCount = 1, System.DateTime? lastAccessed = null)
    {
        return new CacheEntry
        {
            Recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                Priority = OptimizationPriority.Medium,
                Reasoning = "Test"
            },
            ExpiresAt = System.DateTime.Now.AddHours(1),
            CreatedAt = System.DateTime.Now,
            AccessCount = accessCount,
            LastAccessedAt = lastAccessed ?? System.DateTime.Now
        };
    }
}