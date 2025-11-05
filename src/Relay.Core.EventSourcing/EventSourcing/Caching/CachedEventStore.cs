using Relay.Core.EventSourcing.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing.Caching;

/// <summary>
/// Cached decorator for IEventStore that provides caching capabilities.
/// </summary>
public class CachedEventStore : IEventStore
{
    private readonly IEventStore _innerStore;
    private readonly IEventStoreCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedEventStore"/> class.
    /// </summary>
    /// <param name="innerStore">The underlying event store.</param>
    /// <param name="cache">The cache implementation.</param>
    public CachedEventStore(IEventStore innerStore, IEventStoreCache cache)
    {
        _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public async ValueTask SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events, int expectedVersion, CancellationToken cancellationToken = default)
    {
        // Save to the underlying store
        await _innerStore.SaveEventsAsync(aggregateId, events, expectedVersion, cancellationToken);

        // Invalidate cache for this aggregate
        await _cache.InvalidateAsync(aggregateId, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        var cachedEvents = await _cache.GetEventsAsync(aggregateId, cancellationToken);
        if (cachedEvents != null && cachedEvents.Any())
        {
            foreach (var @event in cachedEvents)
            {
                yield return @event;
            }
            yield break;
        }

        // If not in cache, get from underlying store and cache the results
        var events = new List<Event>();
        await foreach (var @event in _innerStore.GetEventsAsync(aggregateId, cancellationToken))
        {
            events.Add(@event);
            yield return @event;
        }

        // Cache the results if any events were found
        if (events.Any())
        {
            await _cache.SetEventsAsync(aggregateId, events, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, int startVersion, int endVersion, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For version range queries, we don't use cache to keep it simple
        // In a production scenario, you might want to implement a more sophisticated caching strategy
        await foreach (var @event in _innerStore.GetEventsAsync(aggregateId, startVersion, endVersion, cancellationToken))
        {
            yield return @event;
        }
    }
}
