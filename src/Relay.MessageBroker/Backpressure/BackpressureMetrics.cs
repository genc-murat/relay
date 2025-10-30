namespace Relay.MessageBroker.Backpressure;

/// <summary>
/// Metrics for backpressure monitoring.
/// </summary>
public sealed class BackpressureMetrics
{
    /// <summary>
    /// Gets or sets the average processing latency.
    /// </summary>
    public TimeSpan AverageLatency { get; set; }

    /// <summary>
    /// Gets or sets the current queue depth.
    /// </summary>
    public int QueueDepth { get; set; }

    /// <summary>
    /// Gets or sets whether throttling is currently active.
    /// </summary>
    public bool IsThrottling { get; set; }

    /// <summary>
    /// Gets or sets the total number of processing records tracked.
    /// </summary>
    public long TotalProcessingRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of times backpressure has been activated.
    /// </summary>
    public long BackpressureActivations { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when backpressure was last activated.
    /// </summary>
    public DateTimeOffset? LastBackpressureActivation { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when backpressure was last deactivated.
    /// </summary>
    public DateTimeOffset? LastBackpressureDeactivation { get; set; }

    /// <summary>
    /// Gets or sets the minimum latency in the current window.
    /// </summary>
    public TimeSpan MinLatency { get; set; }

    /// <summary>
    /// Gets or sets the maximum latency in the current window.
    /// </summary>
    public TimeSpan MaxLatency { get; set; }
}
