using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Exception thrown when a nested transaction configuration is incompatible with the outer transaction
    /// or when nested transaction operations fail.
    /// </summary>
    /// <remarks>
    /// This exception is thrown in scenarios such as:
    /// - Nested transaction has a different isolation level than the outer transaction
    /// - Nested read-write transaction is attempted within a read-only outer transaction
    /// - Nested transaction operations fail in a way that affects the outer transaction
    /// 
    /// <para>Example scenarios:
    /// <code>
    /// // Outer transaction with ReadCommitted
    /// [Transaction(IsolationLevel.ReadCommitted)]
    /// public record OuterCommand : ITransactionalRequest;
    /// 
    /// // Nested transaction with Serializable - will throw NestedTransactionException
    /// [Transaction(IsolationLevel.Serializable)]
    /// public record NestedCommand : ITransactionalRequest;
    /// </code>
    /// </para>
    /// </remarks>
    public class NestedTransactionException : TransactionException
    {
        /// <summary>
        /// Gets the transaction ID of the outer transaction.
        /// </summary>
        public string TransactionId { get; }

        /// <summary>
        /// Gets the nesting level at which the error occurred.
        /// </summary>
        public int NestingLevel { get; }

        /// <summary>
        /// Gets the type of the nested request that caused the exception.
        /// </summary>
        public string RequestType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NestedTransactionException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="transactionId">The transaction ID of the outer transaction.</param>
        /// <param name="nestingLevel">The nesting level at which the error occurred.</param>
        /// <param name="requestType">The type of the nested request that caused the exception.</param>
        public NestedTransactionException(
            string message,
            string transactionId,
            int nestingLevel,
            string requestType)
            : base(message)
        {
            TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
            NestingLevel = nestingLevel;
            RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NestedTransactionException"/> class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="transactionId">The transaction ID of the outer transaction.</param>
        /// <param name="nestingLevel">The nesting level at which the error occurred.</param>
        /// <param name="requestType">The type of the nested request that caused the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public NestedTransactionException(
            string message,
            string transactionId,
            int nestingLevel,
            string requestType,
            Exception innerException)
            : base(message, innerException)
        {
            TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
            NestingLevel = nestingLevel;
            RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        }
    }
}
