using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing
{
    /// <summary>
    /// Implementation of IEventSourcedRepository for event-sourced aggregates.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
    public class EventSourcedRepository<TAggregate, TId> : IEventSourcedRepository<TAggregate, TId>
        where TAggregate : AggregateRoot<TId>, new()
    {
        private readonly IEventStore _eventStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourcedRepository{TAggregate, TId}"/> class.
        /// </summary>
        /// <param name="eventStore">The event store to use.</param>
        public EventSourcedRepository(IEventStore eventStore)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        /// <inheritdoc />
        public async ValueTask<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        {
            var aggregate = new TAggregate();
            
            // Set the ID using reflection (since it's protected)
            var idProperty = typeof(TAggregate).GetProperty(nameof(AggregateRoot<TId>.Id));
            if (idProperty != null && idProperty.CanWrite)
            {
                idProperty.SetValue(aggregate, id);
            }

            // Load events from the event store
            var events = _eventStore.GetEventsAsync(GetAggregateGuid(id), cancellationToken);
            var eventList = new List<Event>();
            
            await foreach (var @event in events.WithCancellation(cancellationToken))
            {
                eventList.Add(@event);
            }

            if (eventList.Count == 0)
            {
                return null;
            }

            // Apply events to the aggregate
            aggregate.LoadFromHistory(eventList);
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

            // Set aggregate ID and version on events
            var aggregateId = GetAggregateGuid(aggregate.Id);
            var expectedVersion = aggregate.Version;
            
            for (int i = 0; i < uncommittedEvents.Count; i++)
            {
                var @event = uncommittedEvents[i];
                @event.AggregateId = aggregateId;
                @event.AggregateVersion = expectedVersion + i + 1;
            }

            // Save events to the event store
            await _eventStore.SaveEventsAsync(aggregateId, uncommittedEvents, expectedVersion, cancellationToken);

            // Clear uncommitted events
            aggregate.ClearUncommittedEvents();

            // Update aggregate version
            if (uncommittedEvents.Count > 0)
            {
                aggregate.GetType().GetProperty(nameof(AggregateRoot<TId>.Version))?
                    .SetValue(aggregate, uncommittedEvents[uncommittedEvents.Count - 1].AggregateVersion);
            }
        }

        private static Guid GetAggregateGuid(TId id)
        {
            if (id is Guid guid)
            {
                return guid;
            }

            // For other types, create a GUID based on the string representation
            return Guid.NewGuid(); // In a real implementation, you would use a more deterministic approach
        }
    }
}