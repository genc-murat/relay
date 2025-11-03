using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Exception thrown when a distributed transaction operation fails.
    /// </summary>
    /// <remarks>
    /// This exception is thrown when:
    /// - Distributed transaction scope creation fails
    /// - Distributed transaction commit fails
    /// - Distributed transaction coordination fails
    /// - MSDTC or equivalent transaction manager is not available
    /// - Network issues prevent distributed transaction coordination
    /// </remarks>
    public class DistributedTransactionException : TransactionException
    {
        /// <summary>
        /// Gets the transaction ID associated with the failed distributed transaction.
        /// </summary>
        public string TransactionId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedTransactionException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DistributedTransactionException(string message)
            : base(message)
        {
            TransactionId = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedTransactionException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DistributedTransactionException(string message, Exception innerException)
            : base(message, innerException)
        {
            TransactionId = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedTransactionException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="transactionId">The transaction ID.</param>
        public DistributedTransactionException(string message, string transactionId)
            : base(message)
        {
            TransactionId = transactionId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedTransactionException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DistributedTransactionException(string message, string transactionId, Exception innerException)
            : base(message, innerException)
        {
            TransactionId = transactionId;
        }
    }
}
