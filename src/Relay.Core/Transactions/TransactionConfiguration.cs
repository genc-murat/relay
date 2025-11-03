using System;
using System.Data;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Concrete implementation of <see cref="ITransactionConfiguration"/>.
    /// </summary>
    internal sealed class TransactionConfiguration : ITransactionConfiguration
    {
        /// <inheritdoc />
        public IsolationLevel IsolationLevel { get; init; }

        /// <inheritdoc />
        public TimeSpan Timeout { get; init; }

        /// <inheritdoc />
        public bool IsReadOnly { get; init; }

        /// <inheritdoc />
        public bool UseDistributedTransaction { get; init; }

        /// <inheritdoc />
        public TransactionRetryPolicy? RetryPolicy { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionConfiguration"/> class.
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction.</param>
        /// <param name="timeout">The timeout duration for the transaction.</param>
        /// <param name="isReadOnly">Whether this is a read-only transaction.</param>
        /// <param name="useDistributedTransaction">Whether to use distributed transaction coordination.</param>
        /// <param name="retryPolicy">The retry policy for transient failures.</param>
        public TransactionConfiguration(
            IsolationLevel isolationLevel,
            TimeSpan timeout,
            bool isReadOnly = false,
            bool useDistributedTransaction = false,
            TransactionRetryPolicy? retryPolicy = null)
        {
            IsolationLevel = isolationLevel;
            Timeout = timeout;
            IsReadOnly = isReadOnly;
            UseDistributedTransaction = useDistributedTransaction;
            RetryPolicy = retryPolicy;
        }
    }
}
