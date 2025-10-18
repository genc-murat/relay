using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI
{
    /// <summary>
    /// Least Recently Used (LRU) cache eviction policy
    /// </summary>
    internal class LruEvictionPolicy : ICacheEvictionPolicy
    {
        private readonly LinkedList<string> _accessOrder = new();
        private readonly Dictionary<string, LinkedListNode<string>> _keyToNode = new();

        public void OnAccess(string key)
        {
            // Move to end (most recently used)
            if (_keyToNode.TryGetValue(key, out var node))
            {
                _accessOrder.Remove(node);
                _accessOrder.AddLast(node);
            }
        }

        public void OnAdd(string key)
        {
            // Add to end (most recently used)
            var node = _accessOrder.AddLast(key);
            _keyToNode[key] = node;
        }

        public void OnRemove(string key)
        {
            if (_keyToNode.TryGetValue(key, out var node))
            {
                _accessOrder.Remove(node);
                _keyToNode.Remove(key);
            }
        }

        public string? GetKeyToEvict(IReadOnlyDictionary<string, CacheEntry> cache)
        {
            // Find the least recently used key that exists in cache
            foreach (var key in _accessOrder)
            {
                if (cache.ContainsKey(key))
                {
                    return key;
                }
            }
            return null;
        }
    }
}