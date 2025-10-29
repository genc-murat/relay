using System;
using System.Collections.Generic;
using System.Threading;
using Json.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.Core.ContractValidation.Caching;

/// <summary>
/// LRU (Least Recently Used) implementation of schema cache with configurable size limits.
/// </summary>
public sealed class LruSchemaCache : ISchemaCache, IDisposable
{
    private readonly SchemaCacheOptions _options;
    private readonly ILogger<LruSchemaCache>? _logger;
    private readonly Dictionary<string, LinkedListNode<CacheEntry>> _cache;
    private readonly LinkedList<CacheEntry> _lruList;
    private readonly ReaderWriterLockSlim _lock;
    private long _totalRequests;
    private long _cacheHits;
    private long _cacheMisses;
    private long _totalEvictions;
    private Timer? _metricsTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the LruSchemaCache class.
    /// </summary>
    /// <param name="options">The cache configuration options.</param>
    /// <param name="logger">Optional logger for cache operations.</param>
    public LruSchemaCache(IOptions<SchemaCacheOptions> options, ILogger<LruSchemaCache>? logger = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _cache = new Dictionary<string, LinkedListNode<CacheEntry>>(_options.MaxCacheSize);
        _lruList = new LinkedList<CacheEntry>();
        _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        if (_options.EnableMetrics && _options.MetricsReportingInterval > TimeSpan.Zero)
        {
            _metricsTimer = new Timer(
                ReportMetrics,
                null,
                _options.MetricsReportingInterval,
                _options.MetricsReportingInterval);
        }

        _logger?.LogInformation(
            "LruSchemaCache initialized with MaxCacheSize={MaxCacheSize}, EnableMetrics={EnableMetrics}",
            _options.MaxCacheSize,
            _options.EnableMetrics);
    }

    /// <inheritdoc />
    public JsonSchema? Get(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
        }

        Interlocked.Increment(ref _totalRequests);

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var node))
            {
                Interlocked.Increment(ref _cacheHits);

                // Move to front (most recently used)
                _lock.EnterWriteLock();
                try
                {
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                _logger?.LogDebug("Cache hit for key: {Key}", key);
                return node.Value.Schema;
            }

            Interlocked.Increment(ref _cacheMisses);
            _logger?.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <inheritdoc />
    public void Set(string key, JsonSchema schema)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
        }

        if (schema == null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        _lock.EnterWriteLock();
        try
        {
            // If key already exists, update it and move to front
            if (_cache.TryGetValue(key, out var existingNode))
            {
                _lruList.Remove(existingNode);
                existingNode.Value = new CacheEntry(key, schema);
                _lruList.AddFirst(existingNode);
                _logger?.LogDebug("Updated existing cache entry for key: {Key}", key);
                return;
            }

            // Check if we need to evict
            if (_cache.Count >= _options.MaxCacheSize)
            {
                EvictLeastRecentlyUsed();
            }

            // Add new entry
            var entry = new CacheEntry(key, schema);
            var node = _lruList.AddFirst(entry);
            _cache[key] = node;

            _logger?.LogDebug("Added new cache entry for key: {Key}", key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
        }

        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _cache.Remove(key);
                _logger?.LogDebug("Removed cache entry for key: {Key}", key);
                return true;
            }

            return false;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            var count = _cache.Count;
            _cache.Clear();
            _lruList.Clear();
            _logger?.LogInformation("Cleared {Count} entries from cache", count);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public SchemaCacheMetrics GetMetrics()
    {
        _lock.EnterReadLock();
        try
        {
            return new SchemaCacheMetrics
            {
                TotalRequests = Interlocked.Read(ref _totalRequests),
                CacheHits = Interlocked.Read(ref _cacheHits),
                CacheMisses = Interlocked.Read(ref _cacheMisses),
                CurrentSize = _cache.Count,
                MaxSize = _options.MaxCacheSize,
                TotalEvictions = Interlocked.Read(ref _totalEvictions)
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Evicts the least recently used entry from the cache.
    /// Must be called within a write lock.
    /// </summary>
    private void EvictLeastRecentlyUsed()
    {
        if (_lruList.Last != null)
        {
            var lruEntry = _lruList.Last.Value;
            _lruList.RemoveLast();
            _cache.Remove(lruEntry.Key);
            Interlocked.Increment(ref _totalEvictions);

            _logger?.LogDebug(
                "Evicted least recently used entry: {Key} (Total evictions: {TotalEvictions})",
                lruEntry.Key,
                _totalEvictions);
        }
    }

    /// <summary>
    /// Reports cache metrics to the logging system.
    /// </summary>
    private void ReportMetrics(object? state)
    {
        if (_logger == null || !_logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var metrics = GetMetrics();
        _logger.LogInformation(
            "Schema Cache Metrics - Hit Rate: {HitRate:P2}, Total Requests: {TotalRequests}, " +
            "Cache Hits: {CacheHits}, Cache Misses: {CacheMisses}, Current Size: {CurrentSize}/{MaxSize}, " +
            "Total Evictions: {TotalEvictions}",
            metrics.HitRate,
            metrics.TotalRequests,
            metrics.CacheHits,
            metrics.CacheMisses,
            metrics.CurrentSize,
            metrics.MaxSize,
            metrics.TotalEvictions);
    }

    /// <summary>
    /// Disposes the cache and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _metricsTimer?.Dispose();
        _lock.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Represents a cache entry with key and schema.
    /// </summary>
    private readonly struct CacheEntry
    {
        public string Key { get; }
        public JsonSchema Schema { get; }

        public CacheEntry(string key, JsonSchema schema)
        {
            Key = key;
            Schema = schema;
        }
    }
}
