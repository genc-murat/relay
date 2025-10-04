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
        public ValueTask SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events, int expectedVersion, CancellationToken cancellationToken = default)
        {
            if (!_events.TryGetValue(aggregateId, out var aggregateEvents))
            {
                aggregateEvents = new List<Event>();
                _events[aggregateId] = aggregateEvents;
            }

            var lastVersion = aggregateEvents.LastOrDefault()?.AggregateVersion ?? -1;

            if (expectedVersion != lastVersion)
            {
                throw new InvalidOperationException("Concurrency conflict: expected version does not match actual version.");
            }

            aggregateEvents.AddRange(events);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
        {
            return GetEventsAsyncImpl(aggregateId, cancellationToken);
            
            async IAsyncEnumerable<Event> GetEventsAsyncImpl(Guid aggregateId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;

                if (_events.TryGetValue(aggregateId, out var aggregateEvents))
                {
                    foreach (var @event in aggregateEvents.OrderBy(e => e.AggregateVersion))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return @event;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, int startVersion, int endVersion, CancellationToken cancellationToken = default)
        {
            return GetEventsAsyncImpl(aggregateId, startVersion, endVersion, cancellationToken);
            
            async IAsyncEnumerable<Event> GetEventsAsyncImpl(Guid aggregateId, int startVersion, int endVersion, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;

                if (_events.TryGetValue(aggregateId, out var aggregateEvents))
                {
                    foreach (var @event in aggregateEvents
                        .Where(e => e.AggregateVersion >= startVersion && e.AggregateVersion <= endVersion)
                        .OrderBy(e => e.AggregateVersion))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return @event;
                    }
                }
            }
        }
    }
}