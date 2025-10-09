using Relay.Core.EventSourcing.Core;
using System;

namespace Relay.Core.EventSourcing.Versioning;

/// <summary>
/// Interface for event migrations to support event schema evolution.
/// </summary>
public interface IEventMigration
{
    /// <summary>
    /// Gets the type of the old event schema.
    /// </summary>
    Type OldEventType { get; }

    /// <summary>
    /// Gets the type of the new event schema.
    /// </summary>
    Type NewEventType { get; }

    /// <summary>
    /// Gets the version from which this migration applies.
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    /// Gets the version to which this migration applies.
    /// </summary>
    int ToVersion { get; }

    /// <summary>
    /// Migrates an event from the old schema to the new schema.
    /// </summary>
    /// <param name="oldEvent">The event with the old schema.</param>
    /// <returns>The event with the new schema.</returns>
    Event Migrate(Event oldEvent);

    /// <summary>
    /// Determines whether this migration can handle the specified event.
    /// </summary>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="version">The version of the event.</param>
    /// <returns>True if this migration can handle the event; otherwise, false.</returns>
    bool CanMigrate(Type eventType, int version);
}
