using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Relay.Core.RateLimiting.Interfaces;

namespace Relay.Core.RateLimiting.Implementations
{
    /// <summary>
    /// In-memory implementation of IRateLimiter using sliding windows.
    /// </summary>
    public class InMemoryRateLimiter : IRateLimiter
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<InMemoryRateLimiter> _logger;

        public InMemoryRateLimiter(IMemoryCache cache, ILogger<InMemoryRateLimiter> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async ValueTask<bool> IsAllowedAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            await Task.CompletedTask; // Make method async for interface compliance

            var cacheKey = $"RateLimit:{key}";

            if (!_cache.TryGetValue(cacheKey, out RateLimitInfo? info) || info is null)
            {
                info = new RateLimitInfo
                {
                    WindowStart = DateTime.UtcNow,
                    RequestCount = 1
                };

                _cache.Set(cacheKey, info, TimeSpan.FromMinutes(5)); // Keep info for 5 minutes
                return true;
            }

            var now = DateTime.UtcNow;
            var windowStart = info.WindowStart;
            var windowEnd = windowStart.AddSeconds(info.WindowSeconds);

            // If we're outside the window, reset
            if (now >= windowEnd)
            {
                info.WindowStart = now;
                info.RequestCount = 1;
                return true;
            }

            // If we're within the window, check if we've exceeded the limit
            if (info.RequestCount < info.RequestsPerWindow)
            {
                info.RequestCount++;
                return true;
            }

            // Rate limit exceeded
            return false;
        }

        /// <inheritdoc />
        public async ValueTask<TimeSpan> GetRetryAfterAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            await Task.CompletedTask; // Make method async for interface compliance

            var cacheKey = $"RateLimit:{key}";

            if (!_cache.TryGetValue(cacheKey, out RateLimitInfo? info) || info is null)
            {
                return TimeSpan.Zero;
            }

            var now = DateTime.UtcNow;
            var windowEnd = info.WindowStart.AddSeconds(info.WindowSeconds);

            if (now >= windowEnd)
            {
                return TimeSpan.Zero;
            }

            return windowEnd - now;
        }

        /// <summary>
        /// Internal class to store rate limit information.
        /// </summary>
        private class RateLimitInfo
        {
            public DateTime WindowStart { get; set; }
            public int RequestCount { get; set; }
            public int RequestsPerWindow { get; set; } = 100; // Default value
            public int WindowSeconds { get; set; } = 60; // Default value
        }
    }
}