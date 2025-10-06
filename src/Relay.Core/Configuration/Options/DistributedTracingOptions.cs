namespace Relay.Core.Configuration.Options;

/// <summary>
/// Configuration options for distributed tracing.
/// </summary>
public class DistributedTracingOptions
{
    /// <summary>
    /// Gets or sets whether to enable automatic distributed tracing for all requests.
    /// </summary>
    public bool EnableAutomaticDistributedTracing { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to trace request processing.
    /// </summary>
    public bool TraceRequests { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to trace response processing.
    /// </summary>
    public bool TraceResponses { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for distributed tracing.
    /// </summary>
    public string ServiceName { get; set; } = "Relay";

    /// <summary>
    /// Gets or sets whether to record exceptions in traces.
    /// </summary>
    public bool RecordExceptions { get; set; } = true;

    /// <summary>
    /// Gets or sets the default order for distributed tracing pipeline behaviors.
    /// </summary>
    public int DefaultOrder { get; set; } = -2500; // Run very early in the pipeline
}
