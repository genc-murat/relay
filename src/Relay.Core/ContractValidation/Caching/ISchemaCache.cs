using Json.Schema;

namespace Relay.Core.ContractValidation.Caching;

/// <summary>
/// Defines a cache for storing parsed JSON schemas.
/// </summary>
public interface ISchemaCache
{
    /// <summary>
    /// Gets a cached schema by key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached schema, or null if not found.</returns>
    JsonSchema? Get(string key);

    /// <summary>
    /// Adds or updates a schema in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="schema">The schema to cache.</param>
    void Set(string key, JsonSchema schema);

    /// <summary>
    /// Removes a schema from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>True if the schema was removed; otherwise, false.</returns>
    bool Remove(string key);

    /// <summary>
    /// Clears all cached schemas.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets current cache metrics.
    /// </summary>
    /// <returns>The cache metrics.</returns>
    SchemaCacheMetrics GetMetrics();
}
