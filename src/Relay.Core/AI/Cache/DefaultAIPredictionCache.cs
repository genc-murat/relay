using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Default implementation of AI prediction cache with in-memory storage.
    /// Implements configurable size limits, eviction policies, and comprehensive statistics.
    /// </summary>
    internal class DefaultAIPredictionCache : IAIPredictionCache, IDisposable
    {
        private readonly ILogger<DefaultAIPredictionCache> _logger;
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly AIPredictionCacheOptions _options;
        private readonly ICacheEvictionPolicy _evictionPolicy;
        private readonly CacheStatistics _statistics;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _cleanupLock = new(1, 1);
        private bool _disposed;

        public DefaultAIPredictionCache(
            ILogger<DefaultAIPredictionCache> logger,
            AIPredictionCacheOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new AIPredictionCacheOptions();
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _statistics = new CacheStatistics();
            _evictionPolicy = CreateEvictionPolicy(_options.EvictionPolicy);

            // Cleanup expired entries at configured interval
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, _options.CleanupInterval, _options.CleanupInterval);

            _logger.LogInformation(
                "AI Prediction Cache initialized with max size: {MaxSize}, cleanup interval: {CleanupInterval}, eviction policy: {Policy}",
                _options.MaxSize, _options.CleanupInterval, _options.EvictionPolicy);
        }

        public ValueTask<OptimizationRecommendation?> GetCachedPredictionAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or whitespace", nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    // Update access tracking
                    entry.LastAccessedAt = DateTime.UtcNow;
                    entry.AccessCount++;
                    _evictionPolicy.OnAccess(key);

                    if (_options.EnableStatistics)
                    {
                        _statistics.RecordHit();
                    }

                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return ValueTask.FromResult<OptimizationRecommendation?>(entry.Recommendation);
                }
                else
                {
                    // Remove expired entry
                    _cache.TryRemove(key, out _);
                    _evictionPolicy.OnRemove(key);

                    if (_options.EnableStatistics)
                    {
                        _statistics.RecordMiss();
                    }

                    _logger.LogDebug("Cache entry expired for key: {Key}", key);
                }
            }
            else
            {
                if (_options.EnableStatistics)
                {
                    _statistics.RecordMiss();
                }
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return ValueTask.FromResult<OptimizationRecommendation?>(null);
        }

        public ValueTask SetCachedPredictionAsync(string key, OptimizationRecommendation recommendation, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or whitespace", nameof(key));
            if (recommendation == null)
                throw new ArgumentNullException(nameof(recommendation));
            if (expiry <= TimeSpan.Zero)
                throw new ArgumentException("Expiry must be positive", nameof(expiry));

            cancellationToken.ThrowIfCancellationRequested();

            var now = DateTime.UtcNow;
            var entry = new CacheEntry
            {
                Recommendation = recommendation,
                ExpiresAt = now.Add(expiry),
                CreatedAt = now,
                LastAccessedAt = now,
                AccessCount = 0
            };

            // Check if we need to evict before adding
            if (_cache.Count >= _options.MaxSize && !_cache.ContainsKey(key))
            {
                EvictEntries();
            }

            var isNewEntry = !_cache.ContainsKey(key);
            _cache.AddOrUpdate(key, entry, (_, __) => entry);

            if (isNewEntry)
            {
                _evictionPolicy.OnAdd(key);
            }
            else
            {
                _evictionPolicy.OnAccess(key);
            }

            if (_options.EnableStatistics)
            {
                _statistics.RecordSet();
            }

            _logger.LogDebug("Cached prediction for key: {Key}, expires at: {ExpiresAt}", key, entry.ExpiresAt);

            return ValueTask.CompletedTask;
        }

        private async void CleanupExpiredEntries(object? state)
        {
            if (!await _cleanupLock.WaitAsync(TimeSpan.FromSeconds(30)))
            {
                _logger.LogWarning("Failed to acquire cleanup lock within timeout");
                return;
            }

            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = new List<string>();

                // Collect expired keys first to avoid modifying collection during enumeration
                foreach (var kvp in _cache)
                {
                    if (kvp.Value.ExpiresAt <= now)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                // Remove expired entries
                foreach (var key in expiredKeys)
                {
                    if (_cache.TryRemove(key, out _))
                    {
                        _evictionPolicy.OnRemove(key);
                    }
                }

                if (_options.EnableStatistics)
                {
                    _statistics.RecordCleanup();
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {ExpiredKeys} expired cache entries", expiredKeys.Count);
                }
            }
            finally
            {
                _cleanupLock.Release();
            }
        }

        private void EvictEntries()
        {
            var keyToEvict = _evictionPolicy.GetKeyToEvict(_cache);
            if (keyToEvict != null && _cache.TryRemove(keyToEvict, out _))
            {
                _evictionPolicy.OnRemove(keyToEvict);

                if (_options.EnableStatistics)
                {
                    _statistics.RecordEviction();
                }

                _logger.LogDebug("Evicted cache entry for key: {Key}", keyToEvict);
            }
        }

        private static ICacheEvictionPolicy CreateEvictionPolicy(CacheEvictionPolicy policy)
        {
            return policy switch
            {
                CacheEvictionPolicy.LRU => new LruEvictionPolicy(),
                CacheEvictionPolicy.LFU => throw new NotImplementedException("LFU policy not yet implemented"),
                CacheEvictionPolicy.FIFO => throw new NotImplementedException("FIFO policy not yet implemented"),
                _ => new LruEvictionPolicy()
            };
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public CacheStatistics GetStatistics() => _statistics;

        /// <summary>
        /// Gets current cache size
        /// </summary>
        public int Size => _cache.Count;

        /// <summary>
        /// Clears all cache entries
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            // Reset eviction policy state if it has a Clear method
            if (_evictionPolicy is LruEvictionPolicy lruPolicy)
            {
                // For now, we recreate the policy. In a more sophisticated implementation,
                // we'd add a Clear method to the interface
                var newPolicy = CreateEvictionPolicy(_options.EvictionPolicy);
                // Note: This is a simplified approach. A proper implementation would
                // add a Clear method to ICacheEvictionPolicy
            }
            _logger.LogInformation("Cache cleared");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _cleanupTimer?.Dispose();
                _cleanupLock?.Dispose();
            }

            _disposed = true;
        }


    }
}
