namespace Relay.Core.Configuration.Options;

/// <summary>
/// Configuration options for event sourcing.
/// </summary>
public class EventSourcingOptions
{
    /// <summary>
    /// Gets or sets whether to enable event sourcing.
    /// </summary>
    public bool EnableEventSourcing { get; set; } = false;

    /// <summary>
    /// Gets or sets the default event store implementation.
    /// </summary>
    public string DefaultEventStore { get; set; } = "InMemory";

    /// <summary>
    /// Gets or sets whether to throw an exception when a concurrency conflict occurs.
    /// </summary>
    public bool ThrowOnConcurrencyConflict { get; set; } = true;

    /// <summary>
    /// Gets or sets the snapshot interval (number of events between snapshots).
    /// </summary>
    public int SnapshotInterval { get; set; } = 100;
}
