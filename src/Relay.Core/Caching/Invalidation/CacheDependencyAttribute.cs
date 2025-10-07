using System;

namespace Relay.Core.Caching.Invalidation;

/// <summary>
/// Attribute for defining cache dependencies between requests.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CacheDependencyAttribute : Attribute
{
    /// <summary>
    /// Gets the dependency key that this cache entry depends on.
    /// </summary>
    public string DependencyKey { get; }

    /// <summary>
    /// Gets the type of dependency.
    /// </summary>
    public CacheDependencyType DependencyType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheDependencyAttribute"/> class.
    /// </summary>
    /// <param name="dependencyKey">The dependency key.</param>
    /// <param name="dependencyType">The type of dependency.</param>
    public CacheDependencyAttribute(string dependencyKey, CacheDependencyType dependencyType = CacheDependencyType.InvalidateOnUpdate)
    {
        DependencyKey = dependencyKey ?? throw new ArgumentNullException(nameof(dependencyKey));
        DependencyType = dependencyType;
    }
}

/// <summary>
/// Types of cache dependencies.
/// </summary>
public enum CacheDependencyType
{
    /// <summary>
    /// Invalidate cache when dependency is updated.
    /// </summary>
    InvalidateOnUpdate,

    /// <summary>
    /// Invalidate cache when dependency is created.
    /// </summary>
    InvalidateOnCreate,

    /// <summary>
    /// Invalidate cache when dependency is deleted.
    /// </summary>
    InvalidateOnDelete,

    /// <summary>
    /// Invalidate cache on any dependency change.
    /// </summary>
    InvalidateOnAnyChange
}