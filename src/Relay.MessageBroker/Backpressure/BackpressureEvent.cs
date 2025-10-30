namespace Relay.MessageBroker.Backpressure;

/// <summary>
/// Event raised when backpressure state changes.
/// </summary>
public sealed class BackpressureEvent
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public BackpressureEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the average latency at the time of the event.
    /// </summary>
    public TimeSpan AverageLatency { get; set; }

    /// <summary>
    /// Gets or sets the queue depth at the time of the event.
    /// </summary>
    public int QueueDepth { get; set; }

    /// <summary>
    /// Gets or sets the reason for the backpressure state change.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Types of backpressure events.
/// </summary>
public enum BackpressureEventType
{
    /// <summary>
    /// Backpressure was activated.
    /// </summary>
    Activated,

    /// <summary>
    /// Backpressure was deactivated (recovered).
    /// </summary>
    Deactivated
}
