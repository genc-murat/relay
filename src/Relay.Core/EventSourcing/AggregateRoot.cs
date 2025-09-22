using System;
using System.Collections.Generic;

namespace Relay.Core.EventSourcing
{
    /// <summary>
    /// Base class for aggregate roots in event sourcing.
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
    public abstract class AggregateRoot<TId>
    {
        private readonly List<Event> _uncommittedEvents = new();

        /// <summary>
        /// Gets the identifier of the aggregate.
        /// </summary>
        public TId Id { get; protected set; } = default!;

        /// <summary>
        /// Gets the version of the aggregate.
        /// </summary>
        public int Version { get; private set; } = -1;

        /// <summary>
        /// Gets the uncommitted events for this aggregate.
        /// </summary>
        public IReadOnlyList<Event> UncommittedEvents => _uncommittedEvents.AsReadOnly();

        /// <summary>
        /// Clears the uncommitted events.
        /// </summary>
        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        /// <summary>
        /// Applies an event to the aggregate.
        /// </summary>
        /// <param name="event">The event to apply.</param>
        protected void Apply(Event @event)
        {
            ((dynamic)this).Apply((dynamic)@event);
            _uncommittedEvents.Add(@event);
        }

        /// <summary>
        /// Loads the aggregate from a sequence of events.
        /// </summary>
        /// <param name="events">The events to load.</param>
        public void LoadFromHistory(IEnumerable<Event> events)
        {
            foreach (var @event in events)
            {
                ((dynamic)this).Apply((dynamic)@event);
                Version = @event.AggregateVersion;
            }
        }
    }
}