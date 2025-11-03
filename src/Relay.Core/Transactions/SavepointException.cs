using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Exception thrown when a savepoint operation fails.
    /// </summary>
    public class SavepointException : TransactionException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SavepointException"/> class.
        /// </summary>
        public SavepointException()
            : base("A savepoint operation failed.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavepointException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SavepointException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavepointException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SavepointException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a specified savepoint is not found.
    /// </summary>
    public class SavepointNotFoundException : SavepointException
    {
        /// <summary>
        /// Gets the name of the savepoint that was not found.
        /// </summary>
        public string SavepointName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavepointNotFoundException"/> class.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint that was not found.</param>
        public SavepointNotFoundException(string savepointName)
            : base($"Savepoint '{savepointName}' was not found.")
        {
            SavepointName = savepointName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavepointNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint that was not found.</param>
        /// <param name="message">The message that describes the error.</param>
        public SavepointNotFoundException(string savepointName, string message)
            : base(message)
        {
            SavepointName = savepointName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavepointNotFoundException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint that was not found.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SavepointNotFoundException(string savepointName, string message, Exception innerException)
            : base(message, innerException)
        {
            SavepointName = savepointName;
        }
    }

    /// <summary>
    /// Exception thrown when attempting to create a savepoint with a name that already exists.
    /// </summary>
    public class SavepointAlreadyExistsException : SavepointException
    {
        /// <summary>
        /// Gets the name of the savepoint that already exists.
        /// </summary>
        public string SavepointName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavepointAlreadyExistsException"/> class.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint that already exists.</param>
        public SavepointAlreadyExistsException(string savepointName)
            : base($"A savepoint with the name '{savepointName}' already exists.")
        {
            SavepointName = savepointName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavepointAlreadyExistsException"/> class with a specified error message.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint that already exists.</param>
        /// <param name="message">The message that describes the error.</param>
        public SavepointAlreadyExistsException(string savepointName, string message)
            : base(message)
        {
            SavepointName = savepointName;
        }
    }
}
