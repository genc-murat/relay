namespace Relay.MessageBroker.Bulkhead;

/// <summary>
/// Metrics for bulkhead pattern.
/// </summary>
public class BulkheadMetrics
{
    /// <summary>
    /// Gets or sets the number of currently active operations.
    /// </summary>
    public int ActiveOperations { get; set; }

    /// <summary>
    /// Gets or sets the number of operations currently queued.
    /// </summary>
    public int QueuedOperations { get; set; }

    /// <summary>
    /// Gets or sets the total number of operations that have been rejected.
    /// </summary>
    public long RejectedOperations { get; set; }

    /// <summary>
    /// Gets or sets the total number of operations that have been executed.
    /// </summary>
    public long ExecutedOperations { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent operations allowed.
    /// </summary>
    public int MaxConcurrentOperations { get; set; }

    /// <summary>
    /// Gets or sets the maximum queued operations allowed.
    /// </summary>
    public int MaxQueuedOperations { get; set; }

    /// <summary>
    /// Gets or sets the average wait time for acquiring a slot.
    /// </summary>
    public TimeSpan AverageWaitTime { get; set; }
}
