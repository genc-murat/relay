using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Exception thrown when a transaction exceeds its configured timeout duration.
    /// </summary>
    /// <remarks>
    /// This exception is thrown when a transaction operation takes longer than the configured timeout period.
    /// The timeout can be configured through <see cref="TransactionOptions.DefaultTimeout"/> or
    /// overridden per request using <see cref="TransactionAttribute.TimeoutSeconds"/>.
    /// 
    /// The exception includes transaction context information to help diagnose timeout issues.
    /// </remarks>
    public class TransactionTimeoutException : TransactionException
    {
        /// <summary>
        /// Gets the transaction ID that timed out.
        /// </summary>
        public string? TransactionId { get; }

        /// <summary>
        /// Gets the configured timeout duration.
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Gets the elapsed time before the timeout occurred.
        /// </summary>
        public TimeSpan Elapsed { get; }

        /// <summary>
        /// Gets the type of request that was executing when the timeout occurred.
        /// </summary>
        public string? RequestType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionTimeoutException"/> class.
        /// </summary>
        public TransactionTimeoutException()
            : base("The transaction operation timed out.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionTimeoutException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TransactionTimeoutException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionTimeoutException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TransactionTimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionTimeoutException"/> class with transaction context information.
        /// </summary>
        /// <param name="transactionId">The ID of the transaction that timed out.</param>
        /// <param name="timeout">The configured timeout duration.</param>
        /// <param name="elapsed">The elapsed time before the timeout occurred.</param>
        /// <param name="requestType">The type of request that was executing.</param>
        public TransactionTimeoutException(
            string transactionId,
            TimeSpan timeout,
            TimeSpan elapsed,
            string? requestType = null)
            : base(BuildMessage(transactionId, timeout, elapsed, requestType))
        {
            TransactionId = transactionId;
            Timeout = timeout;
            Elapsed = elapsed;
            RequestType = requestType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionTimeoutException"/> class with transaction context information
        /// and a reference to the inner exception.
        /// </summary>
        /// <param name="transactionId">The ID of the transaction that timed out.</param>
        /// <param name="timeout">The configured timeout duration.</param>
        /// <param name="elapsed">The elapsed time before the timeout occurred.</param>
        /// <param name="requestType">The type of request that was executing.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TransactionTimeoutException(
            string transactionId,
            TimeSpan timeout,
            TimeSpan elapsed,
            string? requestType,
            Exception innerException)
            : base(BuildMessage(transactionId, timeout, elapsed, requestType), innerException)
        {
            TransactionId = transactionId;
            Timeout = timeout;
            Elapsed = elapsed;
            RequestType = requestType;
        }

        private static string BuildMessage(string transactionId, TimeSpan timeout, TimeSpan elapsed, string? requestType)
        {
            var message = $"Transaction '{transactionId}' timed out after {elapsed.TotalSeconds:F2} seconds. " +
                         $"Configured timeout: {timeout.TotalSeconds:F2} seconds.";

            if (!string.IsNullOrEmpty(requestType))
            {
                message += $" Request type: {requestType}.";
            }

            return message;
        }
    }
}
