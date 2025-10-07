using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Caching.Invalidation;

/// <summary>
/// Interface for cache invalidation strategies.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Invalidates cache entries by key pattern.
    /// </summary>
    Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries by tag.
    /// </summary>
    Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries by dependency.
    /// </summary>
    Task InvalidateByDependencyAsync(string dependencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates a specific cache key.
    /// </summary>
    Task InvalidateByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}