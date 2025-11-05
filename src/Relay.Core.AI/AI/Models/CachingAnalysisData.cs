using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI
{
    /// <summary>
    /// Caching analysis data for tracking cache performance
    /// </summary>
    internal class CachingAnalysisData
    {
        public double CacheHitRate { get; private set; }
        public long TotalAccesses { get; private set; }
        public long CacheHits { get; private set; }
        public DateTime LastAccessTime { get; private set; } = DateTime.UtcNow;

        public int AccessPatternsCount => _accessPatterns.Count;

        private readonly List<AccessPattern> _accessPatterns = new();

        public void AddAccessPatterns(AccessPattern[] patterns)
        {
            _accessPatterns.AddRange(patterns);
            TotalAccesses += patterns.Length;
            CacheHits += patterns.Count(p => p.WasCacheHit);
            CacheHitRate = TotalAccesses > 0 ? (double)CacheHits / TotalAccesses : 0;
            LastAccessTime = DateTime.UtcNow;
        }

        public int CleanupOldAccessPatterns(DateTime cutoffTime)
        {
            var initialCount = _accessPatterns.Count;

            for (int i = _accessPatterns.Count - 1; i >= 0; i--)
            {
                if (_accessPatterns[i].Timestamp < cutoffTime)
                {
                    var pattern = _accessPatterns[i];
                    _accessPatterns.RemoveAt(i);

                    TotalAccesses--;
                    if (pattern.WasCacheHit)
                        CacheHits--;
                }
            }

            CacheHitRate = TotalAccesses > 0 ? (double)CacheHits / TotalAccesses : 0;
            return initialCount - _accessPatterns.Count;
        }
    }
}
