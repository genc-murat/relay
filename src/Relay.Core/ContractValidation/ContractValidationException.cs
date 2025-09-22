using System;
using System.Collections.Generic;

namespace Relay.Core.ContractValidation
{
    /// <summary>
    /// Exception thrown when contract validation fails.
    /// </summary>
    public class ContractValidationException : Exception
    {
        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IEnumerable<string> Errors { get; }

        /// <summary>
        /// Gets the type of the object that failed validation.
        /// </summary>
        public Type ObjectType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractValidationException"/> class.
        /// </summary>
        /// <param name="objectType">The type of the object that failed validation.</param>
        /// <param name="errors">The validation errors.</param>
        public ContractValidationException(Type objectType, IEnumerable<string> errors)
            : base($"Contract validation failed for {objectType.Name}. Errors: {string.Join(", ", errors)}")
        {
            ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractValidationException"/> class.
        /// </summary>
        /// <param name="objectType">The type of the object that failed validation.</param>
        /// <param name="errors">The validation errors.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ContractValidationException(Type objectType, IEnumerable<string> errors, Exception innerException)
            : base($"Contract validation failed for {objectType.Name}. Errors: {string.Join(", ", errors)}", innerException)
        {
            ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }
    }
}