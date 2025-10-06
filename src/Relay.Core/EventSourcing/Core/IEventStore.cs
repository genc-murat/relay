using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing.Core;

/// <summary>
/// Interface for event stores.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Saves events to the event store.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="events">The events to save.</param>
    /// <param name="expectedVersion">The expected version of the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ValueTask representing the completion of the operation.</returns>
    ValueTask SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events, int expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events from the event store.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of events.</returns>
    IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events from the event store within a version range.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="startVersion">The start version (inclusive).</param>
    /// <param name="endVersion">The end version (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of events.</returns>
    IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, int startVersion, int endVersion, CancellationToken cancellationToken = default);
}