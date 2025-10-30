namespace Relay.MessageBroker.Deduplication;

/// <summary>
/// Configuration options for message deduplication.
/// </summary>
public sealed class DeduplicationOptions
{
    /// <summary>
    /// Gets or sets whether deduplication is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the time window for duplicate detection (1 minute to 24 hours).
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum cache size (default: 100,000 entries).
    /// </summary>
    public int MaxCacheSize { get; set; } = 100_000;

    /// <summary>
    /// Gets or sets the deduplication strategy.
    /// </summary>
    public DeduplicationStrategy Strategy { get; set; } = DeduplicationStrategy.ContentHash;

    /// <summary>
    /// Gets or sets the custom hash function (used when Strategy is Custom).
    /// </summary>
    public Func<byte[], string>? CustomHashFunction { get; set; }

    /// <summary>
    /// Validates the deduplication options.
    /// </summary>
    public void Validate()
    {
        if (Window < TimeSpan.FromMinutes(1) || Window > TimeSpan.FromHours(24))
        {
            throw new ArgumentOutOfRangeException(nameof(Window),
                "Window must be between 1 minute and 24 hours");
        }

        if (MaxCacheSize < 1 || MaxCacheSize > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxCacheSize),
                "MaxCacheSize must be between 1 and 1,000,000");
        }

        if (Strategy == DeduplicationStrategy.Custom && CustomHashFunction == null)
        {
            throw new InvalidOperationException(
                "CustomHashFunction must be provided when Strategy is Custom");
        }
    }
}
