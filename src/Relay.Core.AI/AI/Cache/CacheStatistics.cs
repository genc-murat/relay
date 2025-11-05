using System;
using System.Threading;

namespace Relay.Core.AI
{
    /// <summary>
    /// Cache statistics for monitoring and observability
    /// </summary>
    public class CacheStatistics
    {
        private long _hits;
        private long _misses;
        private long _sets;
        private long _evictions;
        private long _cleanups;

        /// <summary>
        /// Number of cache hits
        /// </summary>
        public long Hits => Interlocked.Read(ref _hits);

        /// <summary>
        /// Number of cache misses
        /// </summary>
        public long Misses => Interlocked.Read(ref _misses);

        /// <summary>
        /// Number of cache sets
        /// </summary>
        public long Sets => Interlocked.Read(ref _sets);

        /// <summary>
        /// Number of cache evictions
        /// </summary>
        public long Evictions => Interlocked.Read(ref _evictions);

        /// <summary>
        /// Number of cleanup operations
        /// </summary>
        public long Cleanups => Interlocked.Read(ref _cleanups);

        /// <summary>
        /// Total number of requests (hits + misses)
        /// </summary>
        public long TotalRequests => Hits + Misses;

        /// <summary>
        /// Cache hit ratio (0.0 to 1.0)
        /// </summary>
        public double HitRatio => TotalRequests > 0 ? (double)Hits / TotalRequests : 0.0;

        /// <summary>
        /// Records a cache hit
        /// </summary>
        public void RecordHit() => Interlocked.Increment(ref _hits);

        /// <summary>
        /// Records a cache miss
        /// </summary>
        public void RecordMiss() => Interlocked.Increment(ref _misses);

        /// <summary>
        /// Records a cache set operation
        /// </summary>
        public void RecordSet() => Interlocked.Increment(ref _sets);

        /// <summary>
        /// Records a cache eviction
        /// </summary>
        public void RecordEviction() => Interlocked.Increment(ref _evictions);

        /// <summary>
        /// Records a cleanup operation
        /// </summary>
        public void RecordCleanup() => Interlocked.Increment(ref _cleanups);

        /// <summary>
        /// Resets all statistics
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _hits, 0);
            Interlocked.Exchange(ref _misses, 0);
            Interlocked.Exchange(ref _sets, 0);
            Interlocked.Exchange(ref _evictions, 0);
            Interlocked.Exchange(ref _cleanups, 0);
        }
    }
}