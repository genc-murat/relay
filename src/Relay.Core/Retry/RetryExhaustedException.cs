using System;
using System.Collections.Generic;

namespace Relay.Core.Retry
{
    /// <summary>
    /// Exception thrown when all retry attempts have been exhausted.
    /// </summary>
    public class RetryExhaustedException : Exception
    {
        /// <summary>
        /// Gets the exceptions that occurred during each retry attempt.
        /// </summary>
        public IReadOnlyList<Exception> Exceptions { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryExhaustedException"/> class.
        /// </summary>
        /// <param name="exceptions">The exceptions that occurred during each retry attempt.</param>
        public RetryExhaustedException(IReadOnlyList<Exception> exceptions)
            : base($"Retry attempts exhausted. {exceptions.Count} attempts were made.")
        {
            Exceptions = exceptions ?? throw new ArgumentNullException(nameof(exceptions));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryExhaustedException"/> class.
        /// </summary>
        /// <param name="exceptions">The exceptions that occurred during each retry attempt.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RetryExhaustedException(IReadOnlyList<Exception> exceptions, Exception innerException)
            : base($"Retry attempts exhausted. {exceptions.Count} attempts were made.", innerException)
        {
            Exceptions = exceptions ?? throw new ArgumentNullException(nameof(exceptions));
        }
    }
}