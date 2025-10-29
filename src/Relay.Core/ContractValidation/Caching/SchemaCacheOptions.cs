using System;
using System.Collections.Generic;

namespace Relay.Core.ContractValidation.Caching;

/// <summary>
/// Configuration options for schema caching.
/// </summary>
public sealed class SchemaCacheOptions
{
    /// <summary>
    /// Gets or sets the maximum number of schemas to cache.
    /// Default is 1000.
    /// </summary>
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether cache warming is enabled.
    /// When enabled, frequently used schemas are preloaded during startup.
    /// Default is false.
    /// </summary>
    public bool EnableCacheWarming { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of types to preload during cache warming.
    /// </summary>
    public List<Type> WarmupTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets whether cache metrics collection is enabled.
    /// Default is true.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval at which cache metrics are reported to the logging system.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan MetricsReportingInterval { get; set; } = TimeSpan.FromMinutes(5);
}
