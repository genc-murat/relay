using System;

namespace Relay.Core.Authorization
{
    /// <summary>
    /// Exception thrown when authorization fails.
    /// </summary>
    public class AuthorizationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationException"/> class.
        /// </summary>
        public AuthorizationException()
            : base("Authorization failed.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AuthorizationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationException"/> class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public AuthorizationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}