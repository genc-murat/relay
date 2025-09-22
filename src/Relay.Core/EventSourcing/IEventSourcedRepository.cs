using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing
{
    /// <summary>
    /// Interface for repositories that work with event-sourced aggregates.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
    public interface IEventSourcedRepository<TAggregate, TId>
        where TAggregate : AggregateRoot<TId>, new()
    {
        /// <summary>
        /// Gets an aggregate by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the aggregate.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The aggregate, or null if not found.</returns>
        ValueTask<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves an aggregate to the event store.
        /// </summary>
        /// <param name="aggregate">The aggregate to save.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    }
}