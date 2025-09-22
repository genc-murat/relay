using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.Validation
{
    /// <summary>
    /// Exception thrown when request validation fails.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Gets the type of the request that failed validation.
        /// </summary>
        public Type RequestType { get; }

        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IEnumerable<string> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the ValidationException class.
        /// </summary>
        /// <param name="requestType">The type of the request that failed validation.</param>
        /// <param name="errors">The validation errors.</param>
        public ValidationException(Type requestType, IEnumerable<string> errors)
            : base($"Validation failed for {requestType.Name}. Errors: {string.Join(", ", errors)}")
        {
            RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        /// <summary>
        /// Initializes a new instance of the ValidationException class.
        /// </summary>
        /// <param name="requestType">The type of the request that failed validation.</param>
        /// <param name="errors">The validation errors.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ValidationException(Type requestType, IEnumerable<string> errors, Exception innerException)
            : base($"Validation failed for {requestType.Name}. Errors: {string.Join(", ", errors)}", innerException)
        {
            RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }
    }
}