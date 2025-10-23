using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI
{
    /// <summary>
    /// First In First Out (FIFO) cache eviction policy
    /// </summary>
    internal class FifoEvictionPolicy : ICacheEvictionPolicy
    {
        public void OnAccess(string key)
        {
            // FIFO doesn't track access order, so no-op
        }

        public void OnAdd(string key)
        {
            // FIFO doesn't need to track additions specially
        }

        public void OnRemove(string key)
        {
            // FIFO doesn't maintain state per key
        }

        public string? GetKeyToEvict(IReadOnlyDictionary<string, CacheEntry> cache)
        {
            if (cache.Count == 0)
                return null;

            // Find the oldest entry (smallest CreatedAt)
            return cache
                .OrderBy(kvp => kvp.Value.CreatedAt)
                .First().Key;
        }
    }
}