using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Handles automatic retry of transient transaction failures.
    /// </summary>
    public interface ITransactionRetryHandler
    {
        /// <summary>
        /// Executes an operation with automatic retry on transient failures.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="retryPolicy">The retry policy to apply. If null, no retry is performed.</param>
        /// <param name="transactionId">The transaction ID for logging context.</param>
        /// <param name="requestType">The request type for logging context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="TransactionRetryExhaustedException">
        /// Thrown when all retry attempts are exhausted and the operation continues to fail.
        /// </exception>
        /// <remarks>
        /// If no retry policy is provided, the operation is executed once without retry.
        /// If the operation succeeds on any attempt (including the initial attempt), the result is returned immediately.
        /// </remarks>
        Task<TResult> ExecuteWithRetryAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            TransactionRetryPolicy? retryPolicy,
            string? transactionId,
            string? requestType,
            CancellationToken cancellationToken);
    }
}