using Relay.Core.EventSourcing.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing.Caching;

/// <summary>
/// Interface for event store caching to improve read performance.
/// </summary>
public interface IEventStoreCache
{
    /// <summary>
    /// Gets cached events for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The cached events, or null if not in cache.</returns>
    ValueTask<List<Event>?> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores events in the cache for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="events">The events to cache.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ValueTask representing the completion of the operation.</returns>
    ValueTask SetEventsAsync(Guid aggregateId, List<Event> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cache for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ValueTask representing the completion of the operation.</returns>
    ValueTask InvalidateAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ValueTask representing the completion of the operation.</returns>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
