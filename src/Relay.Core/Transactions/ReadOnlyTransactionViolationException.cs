using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Exception thrown when an attempt is made to modify data within a read-only transaction.
    /// </summary>
    /// <remarks>
    /// This exception is thrown when <see cref="IUnitOfWork.SaveChangesAsync"/> is called
    /// while the unit of work is in read-only mode (<see cref="IUnitOfWork.IsReadOnly"/> is true).
    /// 
    /// Read-only transactions are optimized for query operations and prevent accidental data modifications.
    /// They can be configured using the <see cref="TransactionAttribute.IsReadOnly"/> property.
    /// </remarks>
    public class ReadOnlyTransactionViolationException : TransactionException
    {
        /// <summary>
        /// Gets the transaction ID associated with the read-only transaction.
        /// </summary>
        public string? TransactionId { get; }

        /// <summary>
        /// Gets the type of request that attempted the modification.
        /// </summary>
        public string? RequestType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyTransactionViolationException"/> class.
        /// </summary>
        public ReadOnlyTransactionViolationException()
            : base("Cannot save changes in a read-only transaction.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyTransactionViolationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ReadOnlyTransactionViolationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyTransactionViolationException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ReadOnlyTransactionViolationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyTransactionViolationException"/> class with transaction context information.
        /// </summary>
        /// <param name="transactionId">The ID of the read-only transaction.</param>
        /// <param name="requestType">The type of request that attempted the modification.</param>
        public ReadOnlyTransactionViolationException(string transactionId, string? requestType = null)
            : base(BuildMessage(transactionId, requestType))
        {
            TransactionId = transactionId;
            RequestType = requestType;
        }

        private static string BuildMessage(string transactionId, string? requestType)
        {
            var message = $"Cannot save changes in read-only transaction '{transactionId}'. " +
                         "Read-only transactions are optimized for query operations and do not allow data modifications.";

            if (!string.IsNullOrEmpty(requestType))
            {
                message += $" Request type: {requestType}.";
            }

            return message;
        }
    }
}
