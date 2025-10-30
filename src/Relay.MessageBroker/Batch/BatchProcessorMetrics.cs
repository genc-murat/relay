namespace Relay.MessageBroker.Batch;

/// <summary>
/// Metrics for batch processing operations.
/// </summary>
public sealed class BatchProcessorMetrics
{
    /// <summary>
    /// Gets or sets the current batch size.
    /// </summary>
    public int CurrentBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the average batch size.
    /// </summary>
    public double AverageBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of batches processed.
    /// </summary>
    public long TotalBatchesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages processed.
    /// </summary>
    public long TotalMessagesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the average processing time per batch in milliseconds.
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the success rate (0.0 to 1.0).
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the total number of failed messages.
    /// </summary>
    public long TotalFailedMessages { get; set; }

    /// <summary>
    /// Gets or sets the compression ratio (original size / compressed size).
    /// </summary>
    public double CompressionRatio { get; set; }

    /// <summary>
    /// Gets or sets the last flush timestamp.
    /// </summary>
    public DateTimeOffset? LastFlushAt { get; set; }
}
