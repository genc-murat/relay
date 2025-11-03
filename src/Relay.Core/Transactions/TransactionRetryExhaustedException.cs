using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Exception thrown when a transaction has exhausted all retry attempts.
    /// </summary>
    /// <remarks>
    /// This exception is thrown after all configured retry attempts have been exhausted
    /// and the transaction continues to fail. The inner exception contains the original
    /// exception that caused the final retry attempt to fail.
    /// </remarks>
    public class TransactionRetryExhaustedException : TransactionException
    {
        /// <summary>
        /// Gets the number of retry attempts that were made.
        /// </summary>
        public int RetryAttempts { get; }

        /// <summary>
        /// Gets the transaction ID associated with the failed transaction.
        /// </summary>
        public string? TransactionId { get; }

        /// <summary>
        /// Gets the type of request that was being processed.
        /// </summary>
        public string? RequestType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionRetryExhaustedException"/> class.
        /// </summary>
        /// <param name="retryAttempts">The number of retry attempts that were made.</param>
        /// <param name="innerException">The exception that caused the final retry attempt to fail.</param>
        public TransactionRetryExhaustedException(int retryAttempts, Exception innerException)
            : base($"Transaction failed after {retryAttempts} retry attempts.", innerException)
        {
            RetryAttempts = retryAttempts;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionRetryExhaustedException"/> class.
        /// </summary>
        /// <param name="retryAttempts">The number of retry attempts that were made.</param>
        /// <param name="transactionId">The transaction ID associated with the failed transaction.</param>
        /// <param name="requestType">The type of request that was being processed.</param>
        /// <param name="innerException">The exception that caused the final retry attempt to fail.</param>
        public TransactionRetryExhaustedException(
            int retryAttempts, 
            string? transactionId, 
            string? requestType, 
            Exception innerException)
            : base(
                $"Transaction '{transactionId}' for request type '{requestType}' failed after {retryAttempts} retry attempts. " +
                $"Original error: {innerException.Message}", 
                innerException)
        {
            RetryAttempts = retryAttempts;
            TransactionId = transactionId;
            RequestType = requestType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionRetryExhaustedException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="retryAttempts">The number of retry attempts that were made.</param>
        /// <param name="innerException">The exception that caused the final retry attempt to fail.</param>
        public TransactionRetryExhaustedException(string message, int retryAttempts, Exception innerException)
            : base(message, innerException)
        {
            RetryAttempts = retryAttempts;
        }
    }
}
