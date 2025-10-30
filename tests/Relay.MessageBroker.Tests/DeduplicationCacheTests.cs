using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Relay.MessageBroker.Deduplication;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class DeduplicationCacheTests
{
    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnFalseForNewHash()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act
        var isDuplicate = await cache.IsDuplicateAsync("test-hash");

        // Assert
        Assert.False(isDuplicate);

        cache.Dispose();
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnTrueForExistingHash()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act
        await cache.AddAsync("test-hash", TimeSpan.FromMinutes(5));
        var isDuplicate = await cache.IsDuplicateAsync("test-hash");

        // Assert
        Assert.True(isDuplicate);

        cache.Dispose();
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnFalseForExpiredHash()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act
        await cache.AddAsync("test-hash", TimeSpan.FromMilliseconds(50));
        await Task.Delay(100); // Wait for expiration
        var isDuplicate = await cache.IsDuplicateAsync("test-hash");

        // Assert
        Assert.False(isDuplicate);

        cache.Dispose();
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldThrowWhenHashIsNull()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await cache.IsDuplicateAsync(null!);
        });

        cache.Dispose();
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldThrowWhenHashIsEmpty()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await cache.IsDuplicateAsync(string.Empty);
        });

        cache.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddHashToCache()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act
        await cache.AddAsync("test-hash", TimeSpan.FromMinutes(5));
        var metrics = cache.GetMetrics();

        // Assert
        Assert.Equal(1, metrics.CurrentCacheSize);

        cache.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldThrowWhenHashIsNull()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await cache.AddAsync(null!, TimeSpan.FromMinutes(5));
        });

        cache.Dispose();
    }

    [Fact]
    public async Task GetMetrics_ShouldTrackDuplicateDetection()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act
        await cache.AddAsync("hash1", TimeSpan.FromMinutes(5));
        await cache.IsDuplicateAsync("hash1"); // Duplicate
        await cache.IsDuplicateAsync("hash2"); // Not duplicate
        await cache.IsDuplicateAsync("hash1"); // Duplicate

        var metrics = cache.GetMetrics();

        // Assert
        Assert.Equal(3, metrics.TotalMessagesChecked);
        Assert.Equal(2, metrics.TotalDuplicatesDetected);
        Assert.Equal(2.0 / 3.0, metrics.DuplicateDetectionRate, precision: 2);

        cache.Dispose();
    }

    [Fact]
    public async Task GetMetrics_ShouldCalculateCacheHitRate()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act
        await cache.AddAsync("hash1", TimeSpan.FromMinutes(5));
        await cache.IsDuplicateAsync("hash1"); // Hit
        await cache.IsDuplicateAsync("hash2"); // Miss
        await cache.IsDuplicateAsync("hash1"); // Hit

        var metrics = cache.GetMetrics();

        // Assert
        Assert.Equal(3, metrics.TotalMessagesChecked);
        Assert.Equal(2.0 / 3.0, metrics.CacheHitRate, precision: 2);

        cache.Dispose();
    }

    [Fact]
    public async Task GenerateContentHash_ShouldGenerateConsistentHash()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var hash1 = DeduplicationCache.GenerateContentHash(data);
        var hash2 = DeduplicationCache.GenerateContentHash(data);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task GenerateContentHash_ShouldGenerateDifferentHashForDifferentData()
    {
        // Arrange
        var data1 = new byte[] { 1, 2, 3, 4, 5 };
        var data2 = new byte[] { 1, 2, 3, 4, 6 };

        // Act
        var hash1 = DeduplicationCache.GenerateContentHash(data1);
        var hash2 = DeduplicationCache.GenerateContentHash(data2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateContentHash_ShouldThrowWhenDataIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            DeduplicationCache.GenerateContentHash(null!);
        });
    }

    [Fact]
    public async Task AddAsync_ShouldEnforceCacheSizeLimit()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 10
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);

        // Act
        for (int i = 0; i < 15; i++)
        {
            await cache.AddAsync($"hash-{i}", TimeSpan.FromMinutes(5));
        }

        await Task.Delay(100); // Wait for eviction

        var metrics = cache.GetMetrics();

        // Assert
        Assert.True(metrics.CurrentCacheSize <= options.Value.MaxCacheSize);
        Assert.True(metrics.TotalEvictions > 0);

        cache.Dispose();
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldThrowWhenDisposed()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);
        cache.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await cache.IsDuplicateAsync("test-hash");
        });
    }

    [Fact]
    public async Task AddAsync_ShouldThrowWhenDisposed()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000
        });
        var cache = new DeduplicationCache(options, NullLogger<DeduplicationCache>.Instance);
        cache.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await cache.AddAsync("test-hash", TimeSpan.FromMinutes(5));
        });
    }

    [Fact]
    public async Task Constructor_ShouldThrowWhenOptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new DeduplicationCache(null!, NullLogger<DeduplicationCache>.Instance);
        });
    }

    [Fact]
    public async Task Constructor_ShouldThrowWhenLoggerIsNull()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new DeduplicationCache(options, null!);
        });
    }
}
