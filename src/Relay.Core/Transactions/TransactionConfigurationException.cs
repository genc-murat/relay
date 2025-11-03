using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Exception thrown when transaction configuration is missing or invalid.
    /// </summary>
    /// <remarks>
    /// This exception is typically thrown when:
    /// <list type="bullet">
    /// <item>A transactional request is missing the required <see cref="TransactionAttribute"/></item>
    /// <item>The isolation level is set to <see cref="System.Data.IsolationLevel.Unspecified"/></item>
    /// <item>Configuration values are invalid (e.g., negative timeout)</item>
    /// </list>
    /// </remarks>
    public class TransactionConfigurationException : Exception
    {
        /// <summary>
        /// Gets the type of the request that has invalid configuration.
        /// </summary>
        public Type? RequestType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionConfigurationException"/> class.
        /// </summary>
        public TransactionConfigurationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionConfigurationException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TransactionConfigurationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionConfigurationException"/> class
        /// with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TransactionConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionConfigurationException"/> class
        /// with a specified error message and request type.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="requestType">The type of the request that has invalid configuration.</param>
        public TransactionConfigurationException(string message, Type requestType)
            : base(message)
        {
            RequestType = requestType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionConfigurationException"/> class
        /// with a specified error message, request type, and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="requestType">The type of the request that has invalid configuration.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TransactionConfigurationException(string message, Type requestType, Exception innerException)
            : base(message, innerException)
        {
            RequestType = requestType;
        }
    }
}
