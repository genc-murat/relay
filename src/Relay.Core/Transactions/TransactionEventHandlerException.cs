using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Exception thrown when one or more transaction event handlers fail.
    /// </summary>
    /// <remarks>
    /// This exception is thrown when event handlers fail during transaction lifecycle events.
    /// For BeforeCommit events, this exception will cause the transaction to be rolled back.
    /// For AfterCommit and AfterRollback events, this exception is typically logged but not propagated.
    /// </remarks>
    public class TransactionEventHandlerException : TransactionException
    {
        /// <summary>
        /// Gets the name of the event that failed.
        /// </summary>
        public string EventName { get; }

        /// <summary>
        /// Gets the transaction ID associated with the failed event.
        /// </summary>
        public string TransactionId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionEventHandlerException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="eventName">The name of the event that failed.</param>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="innerException">The inner exception that caused the failure.</param>
        public TransactionEventHandlerException(
            string message,
            string eventName,
            string transactionId,
            Exception? innerException = null)
            : base(message, innerException)
        {
            EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
            TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
        }
    }
}
