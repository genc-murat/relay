using System;

namespace Relay.Core.EventSourcing.Infrastructure;

/// <summary>
/// Database entity for storing events.
/// </summary>
public class EventEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the aggregate.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the version of the aggregate when this event was created.
    /// </summary>
    public int AggregateVersion { get; set; }

    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized event data.
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
