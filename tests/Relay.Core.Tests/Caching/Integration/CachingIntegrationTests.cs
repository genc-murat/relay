using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Caching;
using Relay.Core.Caching.Attributes;
using Relay.Core.Caching.Compression;
using Relay.Core.Caching.Invalidation;
using Relay.Core.Caching.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Caching.Integration;

public class CachingIntegrationTests
{
    [Fact]
    public void CacheComponents_ShouldBeRegisteredAndWork()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add caching
        services.AddMemoryCache();

        // Add custom caching services
        services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
        services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
        services.AddSingleton<ICacheCompressor, GzipCacheCompressor>();
        services.AddSingleton<ICacheMetrics, DefaultCacheMetrics>();
        services.AddSingleton<ICacheInvalidator, DefaultCacheInvalidator>();
        services.AddSingleton<ICacheKeyTracker, InMemoryCacheKeyTracker>();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Test that services can be resolved
        var keyGenerator = serviceProvider.GetRequiredService<ICacheKeyGenerator>();
        var serializer = serviceProvider.GetRequiredService<ICacheSerializer>();
        var compressor = serviceProvider.GetRequiredService<ICacheCompressor>();
        var metrics = serviceProvider.GetRequiredService<ICacheMetrics>();
        var invalidator = serviceProvider.GetRequiredService<ICacheInvalidator>();
        var keyTracker = serviceProvider.GetRequiredService<ICacheKeyTracker>();

        Assert.NotNull(keyGenerator);
        Assert.NotNull(serializer);
        Assert.NotNull(compressor);
        Assert.NotNull(metrics);
        Assert.NotNull(invalidator);
        Assert.NotNull(keyTracker);
    }

    [Fact]
    public void CacheKeyGenerator_ShouldGenerateConsistentKeys()
    {
        // Arrange
        var keyGenerator = new DefaultCacheKeyGenerator();
        var attribute = new RelayCacheAttribute { KeyPattern = "{RequestType}:{RequestHash}" };
        var request = new TestRequest { Id = 1, Name = "Test" };

        // Act
        var key1 = keyGenerator.GenerateKey(request, attribute);
        var key2 = keyGenerator.GenerateKey(request, attribute);

        // Assert
        Assert.Equal(key1, key2);
        Assert.Contains("TestRequest", key1);
    }

    [Fact]
    public void CacheMetrics_ShouldTrackHitsAndMisses()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultCacheMetrics>>();
        var metrics = new DefaultCacheMetrics(loggerMock.Object);

        // Act
        metrics.RecordHit("key1", "TestRequest");
        metrics.RecordMiss("key2", "TestRequest");
        metrics.RecordSet("key3", "TestRequest", 100);

        // Assert
        var stats = metrics.GetStatistics();
        Assert.Equal(1, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(1, stats.Sets);
        Assert.Equal(100, stats.TotalDataSize);
        Assert.Equal(0.5, stats.HitRatio);
    }

    [Fact]
    public void CacheCompressor_ShouldCompressAndDecompress()
    {
        // Arrange
        var compressor = new GzipCacheCompressor(10);
        var originalData = new byte[100];

        // Act
        var compressed = compressor.Compress(originalData);
        var decompressed = compressor.Decompress(compressed);

        // Assert
        Assert.Equal(originalData, decompressed);
        Assert.True(compressed.Length < originalData.Length);
    }

    private class TestRequest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}

public class InMemoryCacheKeyTracker : ICacheKeyTracker
{
    private readonly Dictionary<string, HashSet<string>> _keysByTag = new();
    private readonly Dictionary<string, HashSet<string>> _keysByDependency = new();
    private readonly HashSet<string> _allKeys = new();

    public void AddKey(string key, IEnumerable<string>? tags = null, IEnumerable<string>? dependencies = null)
    {
        _allKeys.Add(key);

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                if (!_keysByTag.ContainsKey(tag))
                    _keysByTag[tag] = new HashSet<string>();
                _keysByTag[tag].Add(key);
            }
        }

        if (dependencies != null)
        {
            foreach (var dependency in dependencies)
            {
                if (!_keysByDependency.ContainsKey(dependency))
                    _keysByDependency[dependency] = new HashSet<string>();
                _keysByDependency[dependency].Add(key);
            }
        }
    }

    public void RemoveKey(string key)
    {
        _allKeys.Remove(key);

        foreach (var tagKeys in _keysByTag.Values)
        {
            tagKeys.Remove(key);
        }

        foreach (var depKeys in _keysByDependency.Values)
        {
            depKeys.Remove(key);
        }
    }

    public IList<string> GetKeysByPattern(string pattern)
    {
        return _allKeys.Where(k => k.Contains(pattern)).ToList();
    }

    public IList<string> GetKeysByTag(string tag)
    {
        return _keysByTag.TryGetValue(tag, out var keys) ? keys.ToList() : new List<string>();
    }

    public IList<string> GetKeysByDependency(string dependencyKey)
    {
        return _keysByDependency.TryGetValue(dependencyKey, out var keys) ? keys.ToList() : new List<string>();
    }

    public IList<string> GetAllKeys()
    {
        return _allKeys.ToList();
    }

    public void ClearAll()
    {
        _allKeys.Clear();
        _keysByTag.Clear();
        _keysByDependency.Clear();
    }
}