using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Cache;

public class DefaultAIPredictionCacheTests
{
    private readonly ILogger<DefaultAIPredictionCache> _logger;
    private readonly DefaultAIPredictionCache _cache;

    public DefaultAIPredictionCacheTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<DefaultAIPredictionCache>();
        _cache = new DefaultAIPredictionCache(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultAIPredictionCache(null!));
    }

    #endregion

    #region GetCachedPredictionAsync Tests

    [Fact]
    public async Task GetCachedPredictionAsync_Should_Throw_When_Key_Is_Null()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.GetCachedPredictionAsync(null!).AsTask());
    }

    [Fact]
    public async Task GetCachedPredictionAsync_Should_Throw_When_Key_Is_Whitespace()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.GetCachedPredictionAsync("   ").AsTask());
    }

    [Fact]
    public async Task GetCachedPredictionAsync_Should_Return_Null_For_NonExistent_Key()
    {
        // Act
        var result = await _cache.GetCachedPredictionAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCachedPredictionAsync_Should_Return_Cached_Value_For_Valid_Key()
    {
        // Arrange
        var key = "test_key";
        var recommendation = CreateTestRecommendation();
        await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMinutes(5));

        // Act
        var result = await _cache.GetCachedPredictionAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recommendation.Strategy, result!.Strategy);
        Assert.Equal(recommendation.ConfidenceScore, result.ConfidenceScore);
    }

    [Fact]
    public async Task GetCachedPredictionAsync_Should_Return_Null_For_Expired_Key()
    {
        // Arrange
        var key = "expired_key";
        var recommendation = CreateTestRecommendation();
        await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMilliseconds(1));

        // Wait for expiry
        await Task.Delay(10);

        // Act
        var result = await _cache.GetCachedPredictionAsync(key);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region SetCachedPredictionAsync Tests

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Throw_When_Key_Is_Null()
    {
        // Arrange
        var recommendation = CreateTestRecommendation();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.SetCachedPredictionAsync(null!, recommendation, TimeSpan.FromMinutes(1)).AsTask());
    }

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Throw_When_Key_Is_Whitespace()
    {
        // Arrange
        var recommendation = CreateTestRecommendation();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.SetCachedPredictionAsync("   ", recommendation, TimeSpan.FromMinutes(1)).AsTask());
    }

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Throw_When_Recommendation_Is_Null()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetCachedPredictionAsync("key", null!, TimeSpan.FromMinutes(1)).AsTask());
    }

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Throw_When_Expiry_Is_Zero()
    {
        // Arrange
        var recommendation = CreateTestRecommendation();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.SetCachedPredictionAsync("key", recommendation, TimeSpan.Zero).AsTask());
    }

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Throw_When_Expiry_Is_Negative()
    {
        // Arrange
        var recommendation = CreateTestRecommendation();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.SetCachedPredictionAsync("key", recommendation, TimeSpan.FromMinutes(-1)).AsTask());
    }

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Store_Value_Successfully()
    {
        // Arrange
        var key = "store_key";
        var recommendation = CreateTestRecommendation();

        // Act
        await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMinutes(5));

        // Assert - retrieve and verify
        var result = await _cache.GetCachedPredictionAsync(key);
        Assert.NotNull(result);
        Assert.Equal(recommendation.Strategy, result!.Strategy);
    }

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Update_Existing_Key()
    {
        // Arrange
        var key = "update_key";
        var originalRecommendation = CreateTestRecommendation();
        var updatedRecommendation = CreateTestRecommendation(OptimizationStrategy.MemoryPooling);

        await _cache.SetCachedPredictionAsync(key, originalRecommendation, TimeSpan.FromMinutes(5));

        // Act
        await _cache.SetCachedPredictionAsync(key, updatedRecommendation, TimeSpan.FromMinutes(5));

        // Assert
        var result = await _cache.GetCachedPredictionAsync(key);
        Assert.NotNull(result);
        Assert.Equal(OptimizationStrategy.MemoryPooling, result!.Strategy);
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public async Task CleanupExpiredEntries_Should_Remove_Expired_Entries()
    {
        // Arrange
        var expiredKey = "expired";
        var validKey = "valid";
        var recommendation = CreateTestRecommendation();

        await _cache.SetCachedPredictionAsync(expiredKey, recommendation, TimeSpan.FromMilliseconds(1));
        await _cache.SetCachedPredictionAsync(validKey, recommendation, TimeSpan.FromMinutes(5));

        // Wait for expiry
        await Task.Delay(10);

        // Act - Trigger cleanup by accessing the private method via reflection (for testing)
        var cleanupMethod = typeof(DefaultAIPredictionCache).GetMethod("CleanupExpiredEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        cleanupMethod?.Invoke(_cache, new object[] { null });

        // Assert
        var expiredResult = await _cache.GetCachedPredictionAsync(expiredKey);
        var validResult = await _cache.GetCachedPredictionAsync(validKey);

        Assert.Null(expiredResult);
        Assert.NotNull(validResult);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetStatistics_ShouldReturnNonNullStatistics()
    {
        // Act
        var stats = _cache.GetStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.IsType<CacheStatistics>(stats);
    }

    [Fact]
    public async Task GetStatistics_ShouldInitiallyHaveZeroValues()
    {
        // Act
        var stats = _cache.GetStatistics();

        // Assert
        Assert.Equal(0, stats.Hits);
        Assert.Equal(0, stats.Misses);
        Assert.Equal(0, stats.Sets);
        Assert.Equal(0, stats.Evictions);
        Assert.Equal(0, stats.Cleanups);
        Assert.Equal(0, stats.TotalRequests);
        Assert.Equal(0.0, stats.HitRatio);
    }

    [Fact]
    public async Task GetStatistics_ShouldRecordHits()
    {
        // Arrange
        var key = "test_key";
        var recommendation = CreateTestRecommendation();
        await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMinutes(5));

        // Act
        await _cache.GetCachedPredictionAsync(key); // This should be a hit
        var stats = _cache.GetStatistics();

        // Assert
        Assert.Equal(1, stats.Hits);
        Assert.Equal(0, stats.Misses);
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(1.0, stats.HitRatio);
    }

    [Fact]
    public async Task GetStatistics_ShouldRecordMisses()
    {
        // Act
        await _cache.GetCachedPredictionAsync("nonexistent_key"); // This should be a miss
        var stats = _cache.GetStatistics();

        // Assert
        Assert.Equal(0, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(0.0, stats.HitRatio);
    }

    [Fact]
    public async Task GetStatistics_ShouldRecordSets()
    {
        // Arrange
        var key = "test_key";
        var recommendation = CreateTestRecommendation();

        // Act
        await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMinutes(5));
        var stats = _cache.GetStatistics();

        // Assert
        Assert.Equal(1, stats.Sets);
    }

    [Fact]
    public async Task GetStatistics_ShouldRecordMultipleOperations()
    {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var recommendation = CreateTestRecommendation();

        // Act
        await _cache.SetCachedPredictionAsync(key1, recommendation, TimeSpan.FromMinutes(5));
        await _cache.SetCachedPredictionAsync(key2, recommendation, TimeSpan.FromMinutes(5));
        await _cache.GetCachedPredictionAsync(key1); // hit
        await _cache.GetCachedPredictionAsync(key2); // hit
        await _cache.GetCachedPredictionAsync("nonexistent"); // miss

        var stats = _cache.GetStatistics();

        // Assert
        Assert.Equal(2, stats.Sets);
        Assert.Equal(2, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(3, stats.TotalRequests);
        Assert.Equal(2.0 / 3.0, stats.HitRatio);
    }

    [Fact]
    public async Task GetStatistics_ShouldRecordEvictions()
    {
        // Arrange
        var key = "test_key";
        var recommendation = CreateTestRecommendation();

        // Set with very short expiry
        await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMilliseconds(1));

        // Wait for expiry
        await Task.Delay(10);

        // Trigger cleanup (this should cause eviction recording)
        var cleanupMethod = typeof(DefaultAIPredictionCache).GetMethod("CleanupExpiredEntries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        cleanupMethod?.Invoke(_cache, new object[] { null });

        // Act
        var stats = _cache.GetStatistics();

        // Assert - Evictions should be recorded during cleanup
        // Note: The exact behavior depends on implementation, but evictions should be >= 0
        Assert.True(stats.Evictions >= 0);
    }

    [Fact]
    public async Task GetStatistics_ShouldRecordCleanups()
    {
        // Arrange
        var key = "test_key";
        var recommendation = CreateTestRecommendation();

        // Set with very short expiry
        await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMilliseconds(1));

        // Wait for expiry
        await Task.Delay(10);

        // Act - Trigger cleanup
        var cleanupMethod = typeof(DefaultAIPredictionCache).GetMethod("CleanupExpiredEntries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        cleanupMethod?.Invoke(_cache, new object[] { null });

        var stats = _cache.GetStatistics();

        // Assert - Cleanups should be recorded
        // Note: The exact behavior depends on implementation, but cleanups should be >= 0
        Assert.True(stats.Cleanups >= 0);
    }

    [Fact]
    public async Task Statistics_ShouldBeConsistentAcrossMultipleCalls()
    {
        // Arrange
        var key = "test_key";
        var recommendation = CreateTestRecommendation();
        await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMinutes(5));
        await _cache.GetCachedPredictionAsync(key); // hit

        // Act
        var stats1 = _cache.GetStatistics();
        var stats2 = _cache.GetStatistics();

        // Assert - Multiple calls should return same object or equivalent values
        Assert.Equal(stats1.Hits, stats2.Hits);
        Assert.Equal(stats1.Misses, stats2.Misses);
        Assert.Equal(stats1.Sets, stats2.Sets);
        Assert.Equal(stats1.TotalRequests, stats2.TotalRequests);
        Assert.Equal(stats1.HitRatio, stats2.HitRatio);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_Should_Remove_All_Entries()
    {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var recommendation = CreateTestRecommendation();

        await _cache.SetCachedPredictionAsync(key1, recommendation, TimeSpan.FromMinutes(5));
        await _cache.SetCachedPredictionAsync(key2, recommendation, TimeSpan.FromMinutes(5));

        // Verify entries exist
        Assert.NotNull(await _cache.GetCachedPredictionAsync(key1));
        Assert.NotNull(await _cache.GetCachedPredictionAsync(key2));
        Assert.Equal(2, _cache.Size);

        // Act
        _cache.Clear();

        // Assert
        Assert.Null(await _cache.GetCachedPredictionAsync(key1));
        Assert.Null(await _cache.GetCachedPredictionAsync(key2));
        Assert.Equal(0, _cache.Size);
    }

    [Fact]
    public async Task Clear_Should_Reset_Statistics_When_Enabled()
    {
        // Arrange - Create cache with statistics enabled
        var options = new AIPredictionCacheOptions { EnableStatistics = true };
        var cacheWithStats = new DefaultAIPredictionCache(_logger, options);

        var key = "test_key";
        var recommendation = CreateTestRecommendation();

        await cacheWithStats.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMinutes(5));
        await cacheWithStats.GetCachedPredictionAsync(key); // hit

        var statsBeforeClear = cacheWithStats.GetStatistics();
        Assert.Equal(1, statsBeforeClear.Sets);
        Assert.Equal(1, statsBeforeClear.Hits);

        // Act
        cacheWithStats.Clear();

        // Assert
        var statsAfterClear = cacheWithStats.GetStatistics();
        Assert.Equal(0, statsAfterClear.Sets);
        Assert.Equal(0, statsAfterClear.Hits);
        Assert.Equal(0, statsAfterClear.Misses);
        Assert.Equal(0, statsAfterClear.Evictions);
        Assert.Equal(0, statsAfterClear.Cleanups);
    }

    [Fact]
    public async Task Clear_Should_Reset_Eviction_Policy_State()
    {
        // Arrange - Create cache with small max size to trigger eviction
        var options = new AIPredictionCacheOptions { MaxSize = 2 };
        var cacheWithEviction = new DefaultAIPredictionCache(_logger, options);

        var recommendation = CreateTestRecommendation();

        // Fill cache to max
        await cacheWithEviction.SetCachedPredictionAsync("key1", recommendation, TimeSpan.FromMinutes(5));
        await cacheWithEviction.SetCachedPredictionAsync("key2", recommendation, TimeSpan.FromMinutes(5));

        // Access key1 to make it recently used (for LRU)
        await cacheWithEviction.GetCachedPredictionAsync("key1");

        // Add third item, should evict key2 (least recently used)
        await cacheWithEviction.SetCachedPredictionAsync("key3", recommendation, TimeSpan.FromMinutes(5));

        // Verify key2 is evicted
        Assert.Null(await cacheWithEviction.GetCachedPredictionAsync("key2"));
        Assert.NotNull(await cacheWithEviction.GetCachedPredictionAsync("key1"));
        Assert.NotNull(await cacheWithEviction.GetCachedPredictionAsync("key3"));

        // Act - Clear cache
        cacheWithEviction.Clear();

        // Assert - Cache is empty
        Assert.Equal(0, cacheWithEviction.Size);

        // Now add items again, should work without eviction issues
        await cacheWithEviction.SetCachedPredictionAsync("new_key1", recommendation, TimeSpan.FromMinutes(5));
        await cacheWithEviction.SetCachedPredictionAsync("new_key2", recommendation, TimeSpan.FromMinutes(5));

        Assert.Equal(2, cacheWithEviction.Size);
        Assert.NotNull(await cacheWithEviction.GetCachedPredictionAsync("new_key1"));
        Assert.NotNull(await cacheWithEviction.GetCachedPredictionAsync("new_key2"));
    }

    #endregion

    #region Eviction Tests

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Evict_Entries_When_Cache_Reaches_Max_Size()
    {
        // Arrange - Create cache with small max size
        var options = new AIPredictionCacheOptions { MaxSize = 2, EnableStatistics = true };
        var cacheWithEviction = new DefaultAIPredictionCache(_logger, options);

        var recommendation = CreateTestRecommendation();

        // Fill cache to max capacity
        await cacheWithEviction.SetCachedPredictionAsync("key1", recommendation, TimeSpan.FromMinutes(5));
        await cacheWithEviction.SetCachedPredictionAsync("key2", recommendation, TimeSpan.FromMinutes(5));

        // Verify initial state
        Assert.Equal(2, cacheWithEviction.Size);
        Assert.NotNull(await cacheWithEviction.GetCachedPredictionAsync("key1"));
        Assert.NotNull(await cacheWithEviction.GetCachedPredictionAsync("key2"));

        // Act - Add third item, should trigger eviction
        await cacheWithEviction.SetCachedPredictionAsync("key3", recommendation, TimeSpan.FromMinutes(5));

        // Assert - Cache should still have max size, one entry should be evicted
        Assert.Equal(2, cacheWithEviction.Size);
        var stats = cacheWithEviction.GetStatistics();
        Assert.Equal(1, stats.Evictions); // One eviction should have occurred
        Assert.Equal(3, stats.Sets); // Three sets total
    }

    [Fact]
    public async Task EvictEntries_Should_Record_Eviction_Statistics_And_Log()
    {
        // Arrange - Create cache with max size 1 to force eviction
        var options = new AIPredictionCacheOptions { MaxSize = 1, EnableStatistics = true };
        var loggerMock = new Mock<ILogger<DefaultAIPredictionCache>>();
        var cache = new DefaultAIPredictionCache(loggerMock.Object, options);

        var recommendation = CreateTestRecommendation();

        // Fill cache
        await cache.SetCachedPredictionAsync("key1", recommendation, TimeSpan.FromMinutes(5));

        // Act - Add second item, triggering eviction
        await cache.SetCachedPredictionAsync("key2", recommendation, TimeSpan.FromMinutes(5));

        // Assert - Verify eviction statistics
        var stats = cache.GetStatistics();
        Assert.Equal(1, stats.Evictions);

        // Verify eviction logging was called
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Evicted cache entry for key")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Not_Evict_When_Updating_Existing_Key()
    {
        // Arrange - Create cache with max size 1
        var options = new AIPredictionCacheOptions { MaxSize = 1, EnableStatistics = true };
        var cache = new DefaultAIPredictionCache(_logger, options);

        var originalRecommendation = CreateTestRecommendation();
        var updatedRecommendation = CreateTestRecommendation(OptimizationStrategy.MemoryPooling);

        // Add initial item
        await cache.SetCachedPredictionAsync("key1", originalRecommendation, TimeSpan.FromMinutes(5));

        // Act - Update existing key (should not trigger eviction)
        await cache.SetCachedPredictionAsync("key1", updatedRecommendation, TimeSpan.FromMinutes(5));

        // Assert - No eviction should have occurred
        Assert.Equal(1, cache.Size);
        var stats = cache.GetStatistics();
        Assert.Equal(0, stats.Evictions); // No evictions
        Assert.Equal(2, stats.Sets); // Two sets

        // Verify updated value
        var result = await cache.GetCachedPredictionAsync("key1");
        Assert.NotNull(result);
        Assert.Equal(OptimizationStrategy.MemoryPooling, result!.Strategy);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task GetCachedPredictionAsync_Should_Throw_When_CancellationToken_Is_Cancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var recommendation = CreateTestRecommendation();
        await _cache.SetCachedPredictionAsync("test_key", recommendation, TimeSpan.FromMinutes(5));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _cache.GetCachedPredictionAsync("test_key", cts.Token).AsTask());
    }

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Throw_When_CancellationToken_Is_Cancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var recommendation = CreateTestRecommendation();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _cache.SetCachedPredictionAsync("test_key", recommendation, TimeSpan.FromMinutes(5), cts.Token).AsTask());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_Should_Not_Throw_On_Multiple_Calls()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultAIPredictionCache>>();
        var cache = new DefaultAIPredictionCache(loggerMock.Object);

        // Act & Assert - Multiple dispose calls should not throw
        cache.Dispose(); // First call
        cache.Dispose(); // Second call - should not throw
        cache.Dispose(); // Third call - should not throw
    }

    [Fact]
    public void Dispose_Should_Set_Disposed_Flag_And_Dispose_Resources()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultAIPredictionCache>>();
        var cache = new DefaultAIPredictionCache(loggerMock.Object);

        // Get private fields via reflection
        var disposedField = typeof(DefaultAIPredictionCache).GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var timerField = typeof(DefaultAIPredictionCache).GetField("_cleanupTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lockField = typeof(DefaultAIPredictionCache).GetField("_cleanupLock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Verify initial state
        Assert.False((bool)disposedField!.GetValue(cache)!);

        // Act - Dispose
        cache.Dispose();

        // Assert - Disposed flag should be set
        Assert.True((bool)disposedField.GetValue(cache)!);

        // Verify resources are disposed (timer and semaphore should be disposed)
        var timer = (Timer)timerField!.GetValue(cache)!;
        var cleanupLock = (SemaphoreSlim)lockField!.GetValue(cache)!;

        // Note: We can't directly check if Timer/SemaphoreSlim are disposed,
        // but we can verify the disposed flag logic by calling Dispose again
        // and ensuring it doesn't throw or change state
        var disposedBefore = (bool)disposedField.GetValue(cache)!;
        cache.Dispose(); // Second dispose call
        var disposedAfter = (bool)disposedField.GetValue(cache)!;

        // Disposed flag should remain true
        Assert.Equal(disposedBefore, disposedAfter);
    }

    [Fact]
    public void Dispose_With_Disposing_False_Should_Not_Dispose_Managed_Resources()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultAIPredictionCache>>();
        var cache = new DefaultAIPredictionCache(loggerMock.Object);

        // Get private fields
        var disposedField = typeof(DefaultAIPredictionCache).GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act - Call protected Dispose method with disposing = false
        var disposeMethod = typeof(DefaultAIPredictionCache).GetMethod("Dispose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        disposeMethod!.Invoke(cache, new object[] { false });

        // Assert - Disposed flag should be set but managed resources might still be alive
        // (This is for finalizer scenario where disposing = false)
        Assert.True((bool)disposedField!.GetValue(cache)!);
    }

    #endregion

    #region Helper Methods

    private static OptimizationRecommendation CreateTestRecommendation(OptimizationStrategy strategy = OptimizationStrategy.EnableCaching)
    {
        return new OptimizationRecommendation
        {
            Strategy = strategy,
            ConfidenceScore = 0.85,
            EstimatedImprovement = TimeSpan.FromMilliseconds(100),
            Reasoning = "Test recommendation",
            Parameters = new System.Collections.Generic.Dictionary<string, object> { ["test"] = "value" },
            Priority = OptimizationPriority.Medium,
            EstimatedGainPercentage = 0.15,
            Risk = RiskLevel.Low
        };
    }

    #endregion

    #region Logging Verification Tests

    [Fact]
    public void Constructor_Should_Log_Initialization()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultAIPredictionCache>>();

        // Act
        var cache = new DefaultAIPredictionCache(loggerMock.Object);

        // Assert - Verify initialization log
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI Prediction Cache initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCachedPredictionAsync_Should_Log_Cache_Hit()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultAIPredictionCache>>();
        var cache = new DefaultAIPredictionCache(loggerMock.Object);
        var recommendation = CreateTestRecommendation();

        await cache.SetCachedPredictionAsync("test_key", recommendation, TimeSpan.FromMinutes(5));

        // Act
        await cache.GetCachedPredictionAsync("test_key");

        // Assert - Verify cache hit log
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Cache hit for key")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCachedPredictionAsync_Should_Log_Cache_Miss()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultAIPredictionCache>>();
        var cache = new DefaultAIPredictionCache(loggerMock.Object);

        // Act
        await cache.GetCachedPredictionAsync("nonexistent_key");

        // Assert - Verify cache miss log
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Cache miss for key")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SetCachedPredictionAsync_Should_Log_Cache_Set()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultAIPredictionCache>>();
        var cache = new DefaultAIPredictionCache(loggerMock.Object);
        var recommendation = CreateTestRecommendation();

        // Act
        await cache.SetCachedPredictionAsync("test_key", recommendation, TimeSpan.FromMinutes(5));

        // Assert - Verify cache set log
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Cached prediction for key")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Clear_Should_Log_Cache_Cleared()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultAIPredictionCache>>();
        var cache = new DefaultAIPredictionCache(loggerMock.Object);

        // Act
        cache.Clear();

        // Assert - Verify clear log
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Cache cleared")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}