namespace Relay.MessageBroker.Inbox;

/// <summary>
/// Configuration options for the Inbox pattern.
/// </summary>
public sealed class InboxOptions
{
    /// <summary>
    /// Gets or sets whether the Inbox pattern is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the retention period for inbox entries.
    /// Minimum value is 24 hours.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the cleanup interval for removing expired inbox entries.
    /// Minimum value is 1 hour.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the consumer name for tracking which consumer processed the message.
    /// If not set, defaults to the machine name.
    /// </summary>
    public string? ConsumerName { get; set; }

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    public void Validate()
    {
        if (RetentionPeriod < TimeSpan.FromHours(24))
        {
            throw new ArgumentException("RetentionPeriod must be at least 24 hours.", nameof(RetentionPeriod));
        }

        if (CleanupInterval < TimeSpan.FromHours(1))
        {
            throw new ArgumentException("CleanupInterval must be at least 1 hour.", nameof(CleanupInterval));
        }
    }
}
