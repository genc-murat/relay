using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Relay.Core.Security
{
    /// <summary>
    /// In-memory implementation of rate limiter using sliding window algorithm.
    /// </summary>
    public class InMemoryRateLimiter : IRateLimiter
    {
        private readonly ConcurrentDictionary<string, RateLimitEntry> _requestCounts = new();
        private readonly int _maxRequestsPerWindow;
        private readonly TimeSpan _windowDuration;

        public InMemoryRateLimiter(int maxRequestsPerWindow = 100, TimeSpan? windowDuration = null)
        {
            _maxRequestsPerWindow = maxRequestsPerWindow;
            _windowDuration = windowDuration ?? TimeSpan.FromMinutes(1);
        }

        public ValueTask<bool> CheckRateLimitAsync(string key)
        {
            var now = DateTimeOffset.UtcNow;
            
            var entry = _requestCounts.AddOrUpdate(
                key,
                _ => new RateLimitEntry 
                { 
                    Count = 1, 
                    WindowStart = now 
                },
                (_, existing) =>
                {
                    // Check if window has expired
                    if (now - existing.WindowStart >= _windowDuration)
                    {
                        // Reset window
                        existing.WindowStart = now;
                        existing.Count = 1;
                    }
                    else
                    {
                        // Increment count in current window
                        existing.Count++;
                    }
                    return existing;
                });

            // Cleanup old entries periodically (simple approach)
            if (_requestCounts.Count > 10000)
            {
                CleanupExpiredEntries();
            }

            return new ValueTask<bool>(entry.Count <= _maxRequestsPerWindow);
        }

        private void CleanupExpiredEntries()
        {
            var now = DateTimeOffset.UtcNow;
            var expiredKeys = _requestCounts
                .Where(kvp => now - kvp.Value.WindowStart >= _windowDuration * 2)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _requestCounts.TryRemove(key, out _);
            }
        }

        private class RateLimitEntry
        {
            public int Count { get; set; }
            public DateTimeOffset WindowStart { get; set; }
        }
    }
}