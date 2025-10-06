namespace Relay.Core.Configuration;

/// <summary>
/// Configuration options for message queuing.
/// </summary>
public class MessageQueueOptions
{
    /// <summary>
    /// Gets or sets whether to enable message queuing.
    /// </summary>
    public bool EnableQueuing { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum queue size.
    /// </summary>
    public int MaxQueueSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the queue processing timeout in seconds.
    /// </summary>
    public int ProcessingTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of concurrent workers to process queue items.
    /// </summary>
    public int ConcurrentWorkers { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to enable dead letter queue for failed messages.
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry attempts for failed messages.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}