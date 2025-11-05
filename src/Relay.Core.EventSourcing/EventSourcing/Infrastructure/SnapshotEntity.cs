using System;

namespace Relay.Core.EventSourcing.Infrastructure;

/// <summary>
/// Database entity for storing aggregate snapshots.
/// </summary>
public class SnapshotEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the snapshot.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the aggregate.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the version of the aggregate at the time of the snapshot.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the type of the aggregate.
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized snapshot data.
    /// </summary>
    public string SnapshotData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the snapshot was created.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
