using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI
{
    /// <summary>
    /// Least Frequently Used (LFU) cache eviction policy
    /// </summary>
    internal class LfuEvictionPolicy : ICacheEvictionPolicy
    {
        private readonly HashSet<string> _trackedKeys = new();

        public void OnAccess(string key)
        {
            // Access counts are tracked in CacheEntry.AccessCount by the cache itself
        }

        public void OnAdd(string key)
        {
            _trackedKeys.Add(key);
        }

        public void OnRemove(string key)
        {
            _trackedKeys.Remove(key);
        }

        public string? GetKeyToEvict(IReadOnlyDictionary<string, CacheEntry> cache)
        {
            if (cache.Count == 0)
                return null;

            // Find the key with the lowest access count among tracked keys
            var minAccessCount = int.MaxValue;
            var candidates = new List<string>();

            foreach (var kvp in cache)
            {
                if (!_trackedKeys.Contains(kvp.Key))
                    continue;

                var accessCount = kvp.Value.AccessCount;
                if (accessCount < minAccessCount)
                {
                    minAccessCount = accessCount;
                    candidates.Clear();
                    candidates.Add(kvp.Key);
                }
                else if (accessCount == minAccessCount)
                {
                    candidates.Add(kvp.Key);
                }
            }

            if (candidates.Count == 0)
                return null;

            // If multiple with same access count, evict the oldest (LRU among LFU)
            if (candidates.Count > 1)
            {
                var oldestKey = candidates
                    .Select(k => (key: k, entry: cache[k]))
                    .OrderBy(x => x.entry.LastAccessedAt)
                    .First().key;
                return oldestKey;
            }

            return candidates.FirstOrDefault();
        }
    }
}