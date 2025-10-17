using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.EventSourcing.Core;

namespace Relay.Core.EventSourcing.Repositories;

/// <summary>
/// Repository that supports both event sourcing and snapshotting for improved performance.
/// </summary>
/// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
/// <typeparam name="TSnapshot">The type of the snapshot.</typeparam>
public class SnapshotRepository<TAggregate, TId, TSnapshot> : IEventSourcedRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>, ISnapshotable<TSnapshot>, new()
    where TSnapshot : class
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore _snapshotStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotRepository{TAggregate, TId, TSnapshot}"/> class.
    /// </summary>
    /// <param name="eventStore">The event store to use.</param>
    /// <param name="snapshotStore">The snapshot store to use.</param>
    public SnapshotRepository(IEventStore eventStore, ISnapshotStore snapshotStore)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
    }

    /// <inheritdoc />
    public async ValueTask<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var aggregate = new TAggregate();
        var aggregateId = GetAggregateGuid(id);

        // Set the ID using reflection
        var idProperty = typeof(TAggregate).GetProperty(nameof(AggregateRoot<TId>.Id));
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(aggregate, id);
        }

        // Try to load from snapshot first
        var snapshotResult = await _snapshotStore.GetSnapshotAsync<TSnapshot>(aggregateId, cancellationToken);
        int startVersion = -1;

        if (snapshotResult.HasValue)
        {
            aggregate.RestoreFromSnapshot(snapshotResult.Value.Snapshot!);
            startVersion = snapshotResult.Value.Version;

            // Set the version from the snapshot store
            var baseType = typeof(AggregateRoot<>).MakeGenericType(typeof(TId));
            var versionProperty = baseType.GetProperty("Version", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (versionProperty != null)
            {
                versionProperty.SetValue(aggregate, snapshotResult.Value.Version, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
            }
        }

        // Load events after the snapshot
        var events = _eventStore.GetEventsAsync(aggregateId, cancellationToken);
        var eventList = new List<Event>();
        var hasEvents = false;

        await foreach (var @event in events.WithCancellation(cancellationToken))
        {
            if (@event.AggregateVersion > startVersion)
            {
                eventList.Add(@event);
                hasEvents = true;
            }
        }

        // If we have neither snapshot nor events, aggregate doesn't exist
        if (!snapshotResult.HasValue && eventList.Count == 0)
        {
            return null;
        }

        // Apply events after snapshot
        if (hasEvents)
        {
            aggregate.LoadFromHistory(eventList);
        }

        aggregate.ClearUncommittedEvents();
        return aggregate;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var uncommittedEvents = aggregate.UncommittedEvents;
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var aggregateId = GetAggregateGuid(aggregate.Id);
        var expectedVersion = aggregate.Version - uncommittedEvents.Count;

        // Save events to the event store
        await _eventStore.SaveEventsAsync(aggregateId, uncommittedEvents, expectedVersion, cancellationToken);

        // Check if we should create a snapshot
        if (aggregate.Version > 0 && aggregate.Version % aggregate.SnapshotFrequency == 0)
        {
            var snapshot = aggregate.CreateSnapshot();
            await _snapshotStore.SaveSnapshotAsync(aggregateId, snapshot, aggregate.Version, cancellationToken);

            // Clean up old snapshots (keep only the latest one for simplicity)
            await _snapshotStore.DeleteOldSnapshotsAsync(aggregateId, aggregate.Version, cancellationToken);
        }

        // Clear uncommitted events
        aggregate.ClearUncommittedEvents();
    }

    private static Guid GetAggregateGuid(TId id)
    {
        if (id is Guid guid)
        {
            return guid;
        }

        // For other types, create a GUID based on the string representation
        // In a real implementation, you would use a more deterministic approach
        throw new NotSupportedException($"Type {typeof(TId)} is not supported as aggregate ID. Use Guid instead.");
    }
}
