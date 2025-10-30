using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Relay.MessageBroker.Deduplication;

/// <summary>
/// Implements message deduplication using an in-memory cache with LRU eviction.
/// </summary>
public sealed class DeduplicationCache : IDeduplicationCache
{
    private readonly ConcurrentDictionary<string, DeduplicationCacheEntry> _cache;
    private readonly DeduplicationOptions _options;
    private readonly ILogger<DeduplicationCache> _logger;
    private readonly Timer _cleanupTimer;
    private readonly SemaphoreSlim _cleanupLock;

    private long _totalMessagesChecked;
    private long _totalDuplicatesDetected;
    private long _totalCacheHits;
    private long _totalEvictions;
    private DateTimeOffset? _lastCleanupAt;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeduplicationCache"/> class.
    /// </summary>
    /// <param name="options">The deduplication options.</param>
    /// <param name="logger">The logger.</param>
    public DeduplicationCache(
        IOptions<DeduplicationOptions> options,
        ILogger<DeduplicationCache> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _cache = new ConcurrentDictionary<string, DeduplicationCacheEntry>();
        _cleanupLock = new SemaphoreSlim(1, 1);

        // Start cleanup timer (run every minute)
        _cleanupTimer = new Timer(
            async _ => await CleanupExpiredEntriesAsync(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));

        _logger.LogInformation(
            "DeduplicationCache initialized with Window={Window}, MaxCacheSize={MaxCacheSize}, Strategy={Strategy}",
            _options.Window,
            _options.MaxCacheSize,
            _options.Strategy);
    }

    /// <inheritdoc/>
    public ValueTask<bool> IsDuplicateAsync(
        string messageHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageHash);
        ObjectDisposedException.ThrowIf(_disposed, this);

        Interlocked.Increment(ref _totalMessagesChecked);

        if (_cache.TryGetValue(messageHash, out var entry))
        {
            Interlocked.Increment(ref _totalCacheHits);

            if (!entry.IsExpired())
            {
                // Update last accessed time for LRU
                entry.LastAccessedAt = DateTimeOffset.UtcNow;

                Interlocked.Increment(ref _totalDuplicatesDetected);

                _logger.LogDebug(
                    "Duplicate message detected: {MessageHash}",
                    messageHash);

                return ValueTask.FromResult(true);
            }

            // Entry expired, remove it
            _cache.TryRemove(messageHash, out _);
        }

        return ValueTask.FromResult(false);
    }

    /// <inheritdoc/>
    public async ValueTask AddAsync(
        string messageHash,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageHash);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entry = new DeduplicationCacheEntry
        {
            MessageHash = messageHash,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
            LastAccessedAt = DateTimeOffset.UtcNow
        };

        _cache.TryAdd(messageHash, entry);

        _logger.LogTrace(
            "Message hash added to cache: {MessageHash}, ExpiresAt={ExpiresAt}",
            messageHash,
            entry.ExpiresAt);

        // Check if cache size exceeds maximum
        if (_cache.Count > _options.MaxCacheSize)
        {
            await EnforceCacheSizeLimitAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public DeduplicationMetrics GetMetrics()
    {
        var totalChecked = Interlocked.Read(ref _totalMessagesChecked);
        var totalDuplicates = Interlocked.Read(ref _totalDuplicatesDetected);
        var totalHits = Interlocked.Read(ref _totalCacheHits);

        return new DeduplicationMetrics
        {
            CurrentCacheSize = _cache.Count,
            TotalMessagesChecked = totalChecked,
            TotalDuplicatesDetected = totalDuplicates,
            DuplicateDetectionRate = totalChecked > 0 ? (double)totalDuplicates / totalChecked : 0.0,
            CacheHitRate = totalChecked > 0 ? (double)totalHits / totalChecked : 0.0,
            TotalEvictions = Interlocked.Read(ref _totalEvictions),
            LastCleanupAt = _lastCleanupAt
        };
    }

    /// <summary>
    /// Generates a SHA256 hash for the given data.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The hash as a hexadecimal string.</returns>
    public static string GenerateContentHash(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Enforces the cache size limit using LRU eviction.
    /// </summary>
    private async ValueTask EnforceCacheSizeLimitAsync(CancellationToken cancellationToken)
    {
        if (!await _cleanupLock.WaitAsync(0, cancellationToken))
        {
            // Another thread is already enforcing the limit
            return;
        }

        try
        {
            var currentSize = _cache.Count;
            if (currentSize <= _options.MaxCacheSize)
            {
                return;
            }

            var targetSize = (int)(_options.MaxCacheSize * 0.9); // Remove 10% to avoid frequent evictions
            var toRemove = currentSize - targetSize;

            _logger.LogDebug(
                "Cache size ({CurrentSize}) exceeds maximum ({MaxSize}), evicting {ToRemove} entries",
                currentSize,
                _options.MaxCacheSize,
                toRemove);

            // Get entries sorted by last accessed time (LRU)
            var entriesToRemove = _cache
                .OrderBy(kvp => kvp.Value.LastAccessedAt)
                .Take(toRemove)
                .Select(kvp => kvp.Key)
                .ToList();

            var evictedCount = 0;
            foreach (var key in entriesToRemove)
            {
                if (_cache.TryRemove(key, out _))
                {
                    evictedCount++;
                }
            }

            Interlocked.Add(ref _totalEvictions, evictedCount);

            _logger.LogInformation(
                "Evicted {EvictedCount} entries from cache using LRU policy",
                evictedCount);
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    /// <summary>
    /// Cleans up expired entries from the cache.
    /// </summary>
    private async Task CleanupExpiredEntriesAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (!await _cleanupLock.WaitAsync(0))
        {
            // Another cleanup is already in progress
            return;
        }

        try
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            if (expiredKeys.Count == 0)
            {
                return;
            }

            _logger.LogDebug(
                "Cleaning up {Count} expired entries from cache",
                expiredKeys.Count);

            var removedCount = 0;
            foreach (var key in expiredKeys)
            {
                if (_cache.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            _lastCleanupAt = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "Removed {RemovedCount} expired entries from cache",
                removedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    /// <summary>
    /// Disposes the deduplication cache.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cleanupTimer.Dispose();
        _cleanupLock.Dispose();
        _cache.Clear();

        _logger.LogInformation("DeduplicationCache disposed");
    }
}
