using System.Collections.Generic;

namespace Relay.Core.Caching.Invalidation;

/// <summary>
/// Interface for tracking cache keys for invalidation purposes.
/// </summary>
public interface ICacheKeyTracker
{
    /// <summary>
    /// Adds a cache key with optional tags and dependencies.
    /// </summary>
    void AddKey(string key, IEnumerable<string>? tags = null, IEnumerable<string>? dependencies = null);

    /// <summary>
    /// Removes a cache key.
    /// </summary>
    void RemoveKey(string key);

    /// <summary>
    /// Gets keys matching a pattern.
    /// </summary>
    IList<string> GetKeysByPattern(string pattern);

    /// <summary>
    /// Gets keys by tag.
    /// </summary>
    IList<string> GetKeysByTag(string tag);

    /// <summary>
    /// Gets keys by dependency.
    /// </summary>
    IList<string> GetKeysByDependency(string dependencyKey);

    /// <summary>
    /// Gets all tracked keys.
    /// </summary>
    IList<string> GetAllKeys();

    /// <summary>
    /// Clears all tracked keys.
    /// </summary>
    void ClearAll();
}