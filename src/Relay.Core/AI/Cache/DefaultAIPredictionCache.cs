using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Default implementation of AI prediction cache with in-memory storage.
    /// </summary>
    internal class DefaultAIPredictionCache : IAIPredictionCache
    {
        private readonly ILogger<DefaultAIPredictionCache> _logger;
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly Timer _cleanupTimer;

        public DefaultAIPredictionCache(ILogger<DefaultAIPredictionCache> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            
            // Cleanup expired entries every 5 minutes
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("AI Prediction Cache initialized");
        }

        public ValueTask<OptimizationRecommendation?> GetCachedPredictionAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or whitespace", nameof(key));

            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return ValueTask.FromResult<OptimizationRecommendation?>(entry.Recommendation);
                }
                else
                {
                    // Remove expired entry
                    _cache.TryRemove(key, out _);
                    _logger.LogDebug("Cache entry expired for key: {Key}", key);
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

            var entry = new CacheEntry
            {
                Recommendation = recommendation,
                ExpiresAt = DateTime.UtcNow.Add(expiry)
            };

            _cache.AddOrUpdate(key, entry, (_, __) => entry);
            
            _logger.LogDebug("Cached prediction for key: {Key}, expires at: {ExpiresAt}", key, entry.ExpiresAt);

            return ValueTask.CompletedTask;
        }

        private void CleanupExpiredEntries(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = 0;

            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    if (_cache.TryRemove(kvp.Key, out _))
                    {
                        expiredKeys++;
                    }
                }
            }

            if (expiredKeys > 0)
            {
                _logger.LogDebug("Cleaned up {ExpiredKeys} expired cache entries", expiredKeys);
            }
        }

        private class CacheEntry
        {
            public OptimizationRecommendation Recommendation { get; init; } = null!;
            public DateTime ExpiresAt { get; init; }
        }
    }
}
