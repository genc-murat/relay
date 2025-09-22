using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing
{
    /// <summary>
    /// In-memory implementation of IEventStore for testing and development.
    /// </summary>
    public class InMemoryEventStore : IEventStore
    {
        private readonly ConcurrentDictionary<Guid, List<Event>> _events = new();

        /// <inheritdoc />
        public async ValueTask SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events, int expectedVersion, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make method async for interface compliance

            if (!_events.TryGetValue(aggregateId, out var aggregateEvents))
            {
                aggregateEvents = new List<Event>();
                _events[aggregateId] = aggregateEvents;
            }

            // Check version concurrency
            if (expectedVersion != -1 && aggregateEvents.Count != expectedVersion + 1)
            {
                throw new InvalidOperationException("Concurrency conflict: expected version does not match actual version.");
            }

            aggregateEvents.AddRange(events);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make method async for interface compliance

            if (_events.TryGetValue(aggregateId, out var aggregateEvents))
            {
                foreach (var @event in aggregateEvents.OrderBy(e => e.AggregateVersion))
                {
                    yield return @event;
                }
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, int startVersion, int endVersion, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make method async for interface compliance

            if (_events.TryGetValue(aggregateId, out var aggregateEvents))
            {
                foreach (var @event in aggregateEvents
                    .Where(e => e.AggregateVersion >= startVersion && e.AggregateVersion <= endVersion)
                    .OrderBy(e => e.AggregateVersion))
                {
                    yield return @event;
                }
            }
        }
    }
}