using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if an object is not null.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    public class RequiredValidationRule<T> : IValidationRule<T> where T : class
    {
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredValidationRule{T}"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public RequiredValidationRule(string errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Value is required.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}