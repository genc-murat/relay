using System;
using System.Threading;
using Json.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation.Caching;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Caching;

public class LruSchemaCacheTests : IDisposable
{
    private readonly LruSchemaCache _cache;
    private readonly SchemaCacheOptions _options;

    public LruSchemaCacheTests()
    {
        _options = new SchemaCacheOptions
        {
            MaxCacheSize = 3,
            EnableMetrics = true,
            MetricsReportingInterval = TimeSpan.FromHours(1) // Long interval to avoid timer firing during tests
        };
        _cache = new LruSchemaCache(Options.Create(_options));
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LruSchemaCache(null!));
    }

    [Fact]
    public void Get_ThrowsArgumentException_WhenKeyIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.Get(null!));
    }

    [Fact]
    public void Get_ThrowsArgumentException_WhenKeyIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.Get(string.Empty));
    }

    [Fact]
    public void Get_ReturnsNull_WhenKeyNotFound()
    {
        // Act
        var result = _cache.Get("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Set_ThrowsArgumentException_WhenKeyIsNull()
    {
        // Arrange
        var schema = JsonSchema.FromText("{}");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.Set(null!, schema));
    }

    [Fact]
    public void Set_ThrowsArgumentException_WhenKeyIsEmpty()
    {
        // Arrange
        var schema = JsonSchema.FromText("{}");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.Set(string.Empty, schema));
    }

    [Fact]
    public void Set_ThrowsArgumentNullException_WhenSchemaIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cache.Set("key", null!));
    }

    [Fact]
    public void Set_And_Get_StoresAndRetrievesSchema()
    {
        // Arrange
        var schema = JsonSchema.FromText("{\"type\": \"string\"}");
        var key = "test-key";

        // Act
        _cache.Set(key, schema);
        var retrieved = _cache.Get(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Same(schema, retrieved);
    }

    [Fact]
    public void Set_UpdatesExistingEntry()
    {
        // Arrange
        var schema1 = JsonSchema.FromText("{\"type\": \"string\"}");
        var schema2 = JsonSchema.FromText("{\"type\": \"number\"}");
        var key = "test-key";

        // Act
        _cache.Set(key, schema1);
        _cache.Set(key, schema2);
        var retrieved = _cache.Get(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Same(schema2, retrieved);
    }

    [Fact]
    public void Remove_ThrowsArgumentException_WhenKeyIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.Remove(null!));
    }

    [Fact]
    public void Remove_ThrowsArgumentException_WhenKeyIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.Remove(string.Empty));
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenKeyNotFound()
    {
        // Act
        var result = _cache.Remove("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Remove_ReturnsTrueAndRemovesEntry_WhenKeyExists()
    {
        // Arrange
        var schema = JsonSchema.FromText("{\"type\": \"string\"}");
        var key = "test-key";
        _cache.Set(key, schema);

        // Act
        var removed = _cache.Remove(key);
        var retrieved = _cache.Get(key);

        // Assert
        Assert.True(removed);
        Assert.Null(retrieved);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        _cache.Set("key1", JsonSchema.FromText("{\"type\": \"string\"}"));
        _cache.Set("key2", JsonSchema.FromText("{\"type\": \"number\"}"));
        _cache.Set("key3", JsonSchema.FromText("{\"type\": \"boolean\"}"));

        // Act
        _cache.Clear();
        var metrics = _cache.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.CurrentSize);
        Assert.Null(_cache.Get("key1"));
        Assert.Null(_cache.Get("key2"));
        Assert.Null(_cache.Get("key3"));
    }

    [Fact]
    public void LruEviction_EvictsLeastRecentlyUsedEntry()
    {
        // Arrange - Cache size is 3
        var schema1 = JsonSchema.FromText("{\"type\": \"string\"}");
        var schema2 = JsonSchema.FromText("{\"type\": \"number\"}");
        var schema3 = JsonSchema.FromText("{\"type\": \"boolean\"}");
        var schema4 = JsonSchema.FromText("{\"type\": \"object\"}");

        // Act
        _cache.Set("key1", schema1);
        _cache.Set("key2", schema2);
        _cache.Set("key3", schema3);
        // This should evict key1 (least recently used)
        _cache.Set("key4", schema4);

        // Assert
        Assert.Null(_cache.Get("key1")); // Evicted
        Assert.NotNull(_cache.Get("key2"));
        Assert.NotNull(_cache.Get("key3"));
        Assert.NotNull(_cache.Get("key4"));
    }

    [Fact]
    public void LruEviction_UpdatesLruOrder_OnGet()
    {
        // Arrange - Cache size is 3
        _cache.Set("key1", JsonSchema.FromText("{\"type\": \"string\"}"));
        _cache.Set("key2", JsonSchema.FromText("{\"type\": \"number\"}"));
        _cache.Set("key3", JsonSchema.FromText("{\"type\": \"boolean\"}"));

        // Act - Access key1 to make it most recently used
        _cache.Get("key1");
        // This should evict key2 (now least recently used)
        _cache.Set("key4", JsonSchema.FromText("{\"type\": \"object\"}"));

        // Assert
        Assert.NotNull(_cache.Get("key1")); // Not evicted because we accessed it
        Assert.Null(_cache.Get("key2")); // Evicted
        Assert.NotNull(_cache.Get("key3"));
        Assert.NotNull(_cache.Get("key4"));
    }

    [Fact]
    public void LruEviction_UpdatesLruOrder_OnSet()
    {
        // Arrange - Cache size is 3
        _cache.Set("key1", JsonSchema.FromText("{\"type\": \"string\"}"));
        _cache.Set("key2", JsonSchema.FromText("{\"type\": \"number\"}"));
        _cache.Set("key3", JsonSchema.FromText("{\"type\": \"boolean\"}"));

        // Act - Update key1 to make it most recently used
        _cache.Set("key1", JsonSchema.FromText("{\"type\": \"array\"}"));
        // This should evict key2 (now least recently used)
        _cache.Set("key4", JsonSchema.FromText("{\"type\": \"object\"}"));

        // Assert
        Assert.NotNull(_cache.Get("key1")); // Not evicted because we updated it
        Assert.Null(_cache.Get("key2")); // Evicted
        Assert.NotNull(_cache.Get("key3"));
        Assert.NotNull(_cache.Get("key4"));
    }

    [Fact]
    public void GetMetrics_ReturnsCorrectMetrics_InitialState()
    {
        // Act
        var metrics = _cache.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.TotalRequests);
        Assert.Equal(0, metrics.CacheHits);
        Assert.Equal(0, metrics.CacheMisses);
        Assert.Equal(0, metrics.CurrentSize);
        Assert.Equal(3, metrics.MaxSize);
        Assert.Equal(0, metrics.TotalEvictions);
        Assert.Equal(0, metrics.HitRate);
    }

    [Fact]
    public void GetMetrics_TracksHitsAndMisses()
    {
        // Arrange
        var schema = JsonSchema.FromText("{\"type\": \"string\"}");
        _cache.Set("key1", schema);

        // Act
        _cache.Get("key1"); // Hit
        _cache.Get("key1"); // Hit
        _cache.Get("key2"); // Miss
        _cache.Get("key3"); // Miss

        var metrics = _cache.GetMetrics();

        // Assert
        Assert.Equal(4, metrics.TotalRequests);
        Assert.Equal(2, metrics.CacheHits);
        Assert.Equal(2, metrics.CacheMisses);
        Assert.Equal(0.5, metrics.HitRate);
    }

    [Fact]
    public void GetMetrics_TracksCurrentSize()
    {
        // Arrange
        _cache.Set("key1", JsonSchema.FromText("{\"type\": \"string\"}"));
        _cache.Set("key2", JsonSchema.FromText("{\"type\": \"number\"}"));

        // Act
        var metrics = _cache.GetMetrics();

        // Assert
        Assert.Equal(2, metrics.CurrentSize);
    }

    [Fact]
    public void GetMetrics_TracksEvictions()
    {
        // Arrange - Cache size is 3
        _cache.Set("key1", JsonSchema.FromText("{\"type\": \"string\"}"));
        _cache.Set("key2", JsonSchema.FromText("{\"type\": \"number\"}"));
        _cache.Set("key3", JsonSchema.FromText("{\"type\": \"boolean\"}"));
        _cache.Set("key4", JsonSchema.FromText("{\"type\": \"object\"}"));
        _cache.Set("key5", JsonSchema.FromText("{\"type\": \"array\"}"));

        // Act
        var metrics = _cache.GetMetrics();

        // Assert
        Assert.Equal(2, metrics.TotalEvictions);
        Assert.Equal(3, metrics.CurrentSize);
    }

    [Fact]
    public void Cache_IsThreadSafe()
    {
        // Arrange
        var threadCount = 10;
        var operationsPerThread = 100;
        var threads = new Thread[threadCount];
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var threadId = i;
            threads[i] = new Thread(() =>
            {
                try
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        var key = $"key-{threadId}-{j}";
                        var schema = JsonSchema.FromText($"{{\"type\": \"string\", \"thread\": {threadId}}}");
                        _cache.Set(key, schema);
                        _cache.Get(key);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
            threads[i].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        Assert.Empty(exceptions);
        var metrics = _cache.GetMetrics();
        Assert.True(metrics.TotalRequests > 0);
    }

    [Fact]
    public void Constructor_WithLogger_LogsInitialization()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LruSchemaCache>>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        // Act
        using var cache = new LruSchemaCache(Options.Create(_options), mockLogger.Object);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LruSchemaCache initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var cache = new LruSchemaCache(Options.Create(_options));

        // Act & Assert - Should not throw
        cache.Dispose();
        cache.Dispose();
    }
}
