namespace Relay.MessageBroker.Outbox;

/// <summary>
/// Configuration options for the Outbox pattern.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Gets or sets whether the Outbox pattern is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the polling interval for checking pending messages.
    /// Minimum value is 100 milliseconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the batch size for retrieving pending messages.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts before marking a message as failed.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff retry strategy.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    public void Validate()
    {
        if (PollingInterval < TimeSpan.FromMilliseconds(100))
        {
            throw new ArgumentException("PollingInterval must be at least 100 milliseconds.", nameof(PollingInterval));
        }

        if (BatchSize <= 0)
        {
            throw new ArgumentException("BatchSize must be greater than 0.", nameof(BatchSize));
        }

        if (MaxRetryAttempts < 0)
        {
            throw new ArgumentException("MaxRetryAttempts must be non-negative.", nameof(MaxRetryAttempts));
        }
    }
}
