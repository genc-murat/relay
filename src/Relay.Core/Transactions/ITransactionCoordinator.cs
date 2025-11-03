using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Interface for coordinating transaction lifecycle operations.
    /// </summary>
    public interface ITransactionCoordinator
    {
        /// <summary>
        /// Begins a new transaction with the specified configuration and timeout enforcement.
        /// </summary>
        /// <param name="configuration">The transaction configuration.</param>
        /// <param name="requestType">The type of request being executed.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>A tuple containing the database transaction, transaction context, and timeout cancellation token source.</returns>
        /// <exception cref="TransactionTimeoutException">Thrown when the transaction times out.</exception>
        Task<(IRelayDbTransaction Transaction, ITransactionContext Context, CancellationTokenSource? TimeoutCts)> BeginTransactionAsync(
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken);

        /// <summary>
        /// Executes an operation within a transaction with timeout enforcement.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="context">The transaction context.</param>
        /// <param name="timeoutCts">The timeout cancellation token source.</param>
        /// <param name="timeout">The configured timeout duration.</param>
        /// <param name="requestType">The type of request being executed.</param>
        /// <param name="cancellationToken">The original cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="TransactionTimeoutException">Thrown when the operation times out.</exception>
        Task<TResult> ExecuteWithTimeoutAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            ITransactionContext context,
            CancellationTokenSource? timeoutCts,
            TimeSpan timeout,
            string requestType,
            CancellationToken cancellationToken);

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="transaction">The database transaction to commit.</param>
        /// <param name="context">The transaction context.</param>
        /// <param name="requestType">The type of request being executed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CommitTransactionAsync(IRelayDbTransaction transaction, ITransactionContext context, string requestType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        /// <param name="transaction">The database transaction to roll back.</param>
        /// <param name="context">The transaction context.</param>
        /// <param name="requestType">The type of request being executed.</param>
        /// <param name="exception">The exception that caused the rollback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RollbackTransactionAsync(
            IRelayDbTransaction transaction,
            ITransactionContext context,
            string requestType,
            Exception? exception = null,
            CancellationToken cancellationToken = default);
    }
}