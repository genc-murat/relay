using System;

namespace Relay.Core.Caching;

/// <summary>
/// Attribute for configuring distributed caching.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DistributedCacheAttribute : Attribute
{
    /// <summary>
    /// Absolute expiration in seconds.
    /// </summary>
    public int AbsoluteExpirationSeconds { get; set; } = 300; // 5 minutes default

    /// <summary>
    /// Sliding expiration in seconds.
    /// </summary>
    public int SlidingExpirationSeconds { get; set; } = 0; // No sliding expiration by default

    /// <summary>
    /// Cache key pattern (supports placeholders).
    /// </summary>
    public string KeyPattern { get; set; } = "{RequestType}:{RequestHash}";

    /// <summary>
    /// Cache regions for logical grouping.
    /// </summary>
    public string Region { get; set; } = "default";

    /// <summary>
    /// Whether to use cache for this request type.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
