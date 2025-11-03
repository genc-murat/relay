using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Interface for publishing transaction lifecycle events to registered event handlers.
    /// </summary>
    public interface ITransactionEventPublisher
    {
        /// <summary>
        /// Publishes the BeforeBegin event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishBeforeBeginAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the AfterBegin event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAfterBeginAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the BeforeCommit event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishBeforeCommitAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the AfterCommit event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAfterCommitAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the BeforeRollback event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishBeforeRollbackAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the AfterRollback event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAfterRollbackAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default);
    }
}