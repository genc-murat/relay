using Relay.Core.EventSourcing.Core;

namespace Relay.Core.EventSourcing.Versioning;

/// <summary>
/// Base class for versioned events that support schema evolution.
/// </summary>
public abstract class VersionedEvent : Event
{
    /// <summary>
    /// Gets or sets the schema version of the event.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Gets the event type identifier used for migration lookups.
    /// </summary>
    public virtual string EventTypeIdentifier => GetType().Name;
}
