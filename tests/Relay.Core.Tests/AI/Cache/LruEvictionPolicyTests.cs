using Relay.Core.AI;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Cache;

public class LruEvictionPolicyTests
{
    private readonly LruEvictionPolicy _policy;

    public LruEvictionPolicyTests()
    {
        _policy = new LruEvictionPolicy();
    }

    [Fact]
    public void OnAdd_AddsKeyToAccessOrder()
    {
        // Act
        _policy.OnAdd("key1");

        // Assert - GetKeyToEvict should return the key when cache contains it
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry()
        };
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void OnAccess_MovesKeyToMostRecentlyUsed()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");
        _policy.OnAdd("key3");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(),
            ["key2"] = CreateTestEntry(),
            ["key3"] = CreateTestEntry()
        };

        // Initially, key1 should be evicted first (least recently used)
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));

        // Act - Access key1
        _policy.OnAccess("key1");

        // Assert - Now key2 should be evicted first
        Assert.Equal("key2", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void OnRemove_RemovesKeyFromTracking()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(),
            ["key2"] = CreateTestEntry()
        };

        // Initially, key1 should be evicted first
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));

        // Act
        _policy.OnRemove("key1");

        // Assert - Now key2 should be evicted first
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
            ["key3"] = CreateTestEntry() // Different key not tracked by policy
        };

        // Act
        var result = _policy.GetKeyToEvict(cache);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetKeyToEvict_ReturnsLeastRecentlyUsedKey()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");
        _policy.OnAdd("key3");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(),
            ["key2"] = CreateTestEntry(),
            ["key3"] = CreateTestEntry()
        };

        // Act
        var result = _policy.GetKeyToEvict(cache);

        // Assert
        Assert.Equal("key1", result);
    }

    [Fact]
    public void GetKeyToEvict_SkipsKeysNotInCache()
    {
        // Arrange
        _policy.OnAdd("key1"); // Will be removed from cache
        _policy.OnAdd("key2");
        _policy.OnAdd("key3");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key2"] = CreateTestEntry(),
            ["key3"] = CreateTestEntry()
            // key1 is not in cache
        };

        // Act
        var result = _policy.GetKeyToEvict(cache);

        // Assert - Should skip key1 and return key2
        Assert.Equal("key2", result);
    }

    [Fact]
    public void OnAccess_DoesNothing_ForUnknownKey()
    {
        // Arrange
        _policy.OnAdd("key1");
        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry()
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
            ["key1"] = CreateTestEntry(),
            ["key2"] = CreateTestEntry()
        };

        // Act - Remove unknown key
        _policy.OnRemove("unknown");

        // Assert - Should not affect eviction order
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void ComplexAccessPattern_MaintainsCorrectOrder()
    {
        // Arrange
        _policy.OnAdd("a");
        _policy.OnAdd("b");
        _policy.OnAdd("c");
        _policy.OnAdd("d");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["a"] = CreateTestEntry(),
            ["b"] = CreateTestEntry(),
            ["c"] = CreateTestEntry(),
            ["d"] = CreateTestEntry()
        };

        // Act - Access 'a' (moves to end)
        _policy.OnAccess("a");

        // Assert - 'b' should now be LRU
        Assert.Equal("b", _policy.GetKeyToEvict(cache));

        // Act - Access 'c' (moves to end)
        _policy.OnAccess("c");

        // Assert - 'b' should still be LRU
        Assert.Equal("b", _policy.GetKeyToEvict(cache));

        // Act - Access 'b' (moves to end)
        _policy.OnAccess("b");

        // Assert - 'd' should now be LRU
        Assert.Equal("d", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void RemoveAndReAddKey_ResetsPosition()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(),
            ["key2"] = CreateTestEntry()
        };

        // Initially key1 is LRU
        Assert.Equal("key1", _policy.GetKeyToEvict(cache));

        // Act - Remove and re-add key1
        _policy.OnRemove("key1");
        _policy.OnAdd("key1");

        // Assert - key1 should now be MRU (most recently added)
        Assert.Equal("key2", _policy.GetKeyToEvict(cache));
    }

    [Fact]
    public void MultipleAccesses_MoveKeyToEnd()
    {
        // Arrange
        _policy.OnAdd("key1");
        _policy.OnAdd("key2");
        _policy.OnAdd("key3");

        var cache = new Dictionary<string, CacheEntry>
        {
            ["key1"] = CreateTestEntry(),
            ["key2"] = CreateTestEntry(),
            ["key3"] = CreateTestEntry()
        };

        // Act - Access key1 multiple times
        _policy.OnAccess("key1");
        _policy.OnAccess("key1");
        _policy.OnAccess("key1");

        // Assert - key1 should be MRU, key2 should be LRU
        Assert.Equal("key2", _policy.GetKeyToEvict(cache));
    }

    private static CacheEntry CreateTestEntry()
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
            AccessCount = 1,
            LastAccessedAt = System.DateTime.Now
        };
    }
}