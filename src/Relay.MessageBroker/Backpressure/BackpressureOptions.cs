namespace Relay.MessageBroker.Backpressure;

/// <summary>
/// Configuration options for backpressure management.
/// </summary>
public sealed class BackpressureOptions
{
    /// <summary>
    /// Gets or sets whether backpressure management is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the latency threshold that triggers backpressure.
    /// When average processing latency exceeds this threshold, backpressure is activated.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan LatencyThreshold { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the queue depth threshold that triggers backpressure.
    /// When queue depth exceeds this threshold, backpressure is activated.
    /// Default is 10,000 messages.
    /// </summary>
    public int QueueDepthThreshold { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the recovery latency threshold.
    /// When average processing latency falls below this threshold, backpressure is deactivated.
    /// Default is 2 seconds.
    /// </summary>
    public TimeSpan RecoveryLatencyThreshold { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the size of the sliding window for latency calculation.
    /// Default is 100 samples.
    /// </summary>
    public int SlidingWindowSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the throttle factor when backpressure is active.
    /// This represents the percentage reduction in consumption rate (0.0 to 1.0).
    /// Default is 0.5 (50% reduction).
    /// </summary>
    public double ThrottleFactor { get; set; } = 0.5;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when options are invalid.</exception>
    public void Validate()
    {
        if (LatencyThreshold <= TimeSpan.Zero)
        {
            throw new ArgumentException("LatencyThreshold must be greater than zero.", nameof(LatencyThreshold));
        }

        if (RecoveryLatencyThreshold <= TimeSpan.Zero)
        {
            throw new ArgumentException("RecoveryLatencyThreshold must be greater than zero.", nameof(RecoveryLatencyThreshold));
        }

        if (RecoveryLatencyThreshold >= LatencyThreshold)
        {
            throw new ArgumentException(
                "RecoveryLatencyThreshold must be less than LatencyThreshold.",
                nameof(RecoveryLatencyThreshold));
        }

        if (QueueDepthThreshold <= 0)
        {
            throw new ArgumentException("QueueDepthThreshold must be greater than zero.", nameof(QueueDepthThreshold));
        }

        if (SlidingWindowSize <= 0)
        {
            throw new ArgumentException("SlidingWindowSize must be greater than zero.", nameof(SlidingWindowSize));
        }

        if (ThrottleFactor < 0.0 || ThrottleFactor > 1.0)
        {
            throw new ArgumentException("ThrottleFactor must be between 0.0 and 1.0.", nameof(ThrottleFactor));
        }
    }
}
