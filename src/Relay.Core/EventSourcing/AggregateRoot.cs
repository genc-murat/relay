using System;
using System.Collections.Generic;

namespace Relay.Core.EventSourcing
{
    public abstract class AggregateRoot<TId>
    {
        private readonly List<Event> _uncommittedEvents = new();

        public TId Id { get; protected set; } = default!;

        public int Version { get; private set; } = -1;

        public IReadOnlyList<Event> UncommittedEvents => _uncommittedEvents.AsReadOnly();

        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        protected void Apply(Event @event)
        {
            ApplyChange(@event);
            Version = @event.AggregateVersion;
            _uncommittedEvents.Add(@event);
        }

        public void LoadFromHistory(IEnumerable<Event> events)
        {
            foreach (var @event in events)
            {
                ApplyChange(@event);
                Version = @event.AggregateVersion;
            }
        }

        private void ApplyChange(Event @event)
        {
            ((dynamic)this).When((dynamic)@event);
        }
    }
}