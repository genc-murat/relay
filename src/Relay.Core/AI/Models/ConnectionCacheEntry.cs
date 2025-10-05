using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Connection cache entry
    /// </summary>
    internal class ConnectionCacheEntry
    {
        public int Count { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
