namespace Relay.MessageBroker.PoisonMessage;

/// <summary>
/// Configuration options for poison message handling.
/// </summary>
public sealed class PoisonMessageOptions
{
    /// <summary>
    /// Gets or sets whether poison message handling is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the failure threshold before a message is considered poisonous.
    /// Default is 5 failures.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the retention period for poison messages.
    /// Default is 7 days.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the cleanup interval for removing expired poison messages.
    /// Default is 1 hour.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
