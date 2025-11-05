using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for cache eviction policies
    /// </summary>
    public interface ICacheEvictionPolicy
    {
        /// <summary>
        /// Called when an entry is accessed
        /// </summary>
        void OnAccess(string key);

        /// <summary>
        /// Called when an entry is added
        /// </summary>
        void OnAdd(string key);

        /// <summary>
        /// Called when an entry is removed
        /// </summary>
        void OnRemove(string key);

        /// <summary>
        /// Gets the key to evict based on the policy
        /// </summary>
        string? GetKeyToEvict(IReadOnlyDictionary<string, CacheEntry> cache);
    }
}