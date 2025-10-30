namespace Relay.MessageBroker.Batch;

/// <summary>
/// Configuration options for batch processing.
/// </summary>
public sealed class BatchOptions
{
    /// <summary>
    /// Gets or sets whether batch processing is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size (1-10000).
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the flush interval for time-based batching.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets whether batch compression is enabled.
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Gets or sets whether partial retry is enabled for failed messages in a batch.
    /// </summary>
    public bool PartialRetry { get; set; } = true;

    /// <summary>
    /// Validates the batch options.
    /// </summary>
    public void Validate()
    {
        if (MaxBatchSize < 1 || MaxBatchSize > 10000)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxBatchSize), 
                "MaxBatchSize must be between 1 and 10000");
        }

        if (FlushInterval < TimeSpan.FromMilliseconds(1))
        {
            throw new ArgumentOutOfRangeException(nameof(FlushInterval), 
                "FlushInterval must be at least 1 millisecond");
        }
    }
}
