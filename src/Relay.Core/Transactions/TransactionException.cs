using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Base exception class for all transaction-related exceptions.
    /// </summary>
    /// <remarks>
    /// This is the base class for all exceptions thrown by the transaction system.
    /// Specific transaction errors derive from this class to provide more detailed information.
    /// </remarks>
    public class TransactionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class.
        /// </summary>
        public TransactionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TransactionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TransactionException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
