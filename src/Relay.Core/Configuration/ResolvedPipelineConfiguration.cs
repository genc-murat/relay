using System;

namespace Relay.Core.Configuration;

/// <summary>
/// Resolved configuration for a pipeline behavior.
/// </summary>
public class ResolvedPipelineConfiguration
{
    /// <summary>
    /// Gets the execution order of the pipeline behavior.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets the scope of the pipeline behavior.
    /// </summary>
    public PipelineScope Scope { get; set; }

    /// <summary>
    /// Gets whether caching is enabled for this pipeline.
    /// </summary>
    public bool EnableCaching { get; set; }

    /// <summary>
    /// Gets the timeout for pipeline execution.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}
