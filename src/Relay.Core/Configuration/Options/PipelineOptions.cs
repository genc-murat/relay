using System;

namespace Relay.Core.Configuration.Options;

/// <summary>
/// Configuration options for pipeline behaviors.
/// </summary>
public class PipelineOptions
{
    /// <summary>
    /// Gets or sets the default order for pipeline behaviors.
    /// </summary>
    public int DefaultOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets the default scope for pipeline behaviors.
    /// </summary>
    public PipelineScope DefaultScope { get; set; } = PipelineScope.All;

    /// <summary>
    /// Gets or sets whether to enable pipeline caching.
    /// </summary>
    public bool EnableCaching { get; set; } = false;

    /// <summary>
    /// Gets or sets the default timeout for pipeline execution.
    /// </summary>
    public TimeSpan? DefaultTimeout { get; set; }
}
