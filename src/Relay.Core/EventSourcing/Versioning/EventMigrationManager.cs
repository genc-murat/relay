using Relay.Core.EventSourcing.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.EventSourcing.Versioning;

/// <summary>
/// Manages event migrations for schema evolution.
/// </summary>
public class EventMigrationManager
{
    private readonly List<IEventMigration> _migrations = new();

    /// <summary>
    /// Registers an event migration.
    /// </summary>
    /// <param name="migration">The migration to register.</param>
    public void RegisterMigration(IEventMigration migration)
    {
        if (migration == null)
        {
            throw new ArgumentNullException(nameof(migration));
        }

        _migrations.Add(migration);
    }

    /// <summary>
    /// Registers multiple event migrations.
    /// </summary>
    /// <param name="migrations">The migrations to register.</param>
    public void RegisterMigrations(IEnumerable<IEventMigration> migrations)
    {
        if (migrations == null)
        {
            throw new ArgumentNullException(nameof(migrations));
        }

        foreach (var migration in migrations)
        {
            RegisterMigration(migration);
        }
    }

    /// <summary>
    /// Migrates an event to the latest schema version.
    /// </summary>
    /// <param name="event">The event to migrate.</param>
    /// <returns>The migrated event.</returns>
    public Event MigrateEvent(Event @event)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        var currentEvent = @event;
        var eventType = @event.GetType();
        var version = (@event as VersionedEvent)?.SchemaVersion ?? 1;

        // Find applicable migrations and apply them in order
        var applicableMigrations = _migrations
            .Where(m => m.CanMigrate(eventType, version))
            .OrderBy(m => m.FromVersion)
            .ToList();

        foreach (var migration in applicableMigrations)
        {
            currentEvent = migration.Migrate(currentEvent);
            eventType = currentEvent.GetType();
            version = (currentEvent as VersionedEvent)?.SchemaVersion ?? 1;
        }

        return currentEvent;
    }

    /// <summary>
    /// Migrates a collection of events to the latest schema versions.
    /// </summary>
    /// <param name="events">The events to migrate.</param>
    /// <returns>The migrated events.</returns>
    public IEnumerable<Event> MigrateEvents(IEnumerable<Event> events)
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        return events.Select(MigrateEvent);
    }

    /// <summary>
    /// Gets the number of registered migrations.
    /// </summary>
    public int MigrationCount => _migrations.Count;

    /// <summary>
    /// Clears all registered migrations.
    /// </summary>
    public void Clear()
    {
        _migrations.Clear();
    }
}
