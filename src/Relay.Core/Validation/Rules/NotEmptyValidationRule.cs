using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is not null or empty.
    /// </summary>
    public class NotEmptyValidationRule : IValidationRule<string>
    {
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotEmptyValidationRule"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public NotEmptyValidationRule(string? errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Value cannot be null or empty.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }

    /// <summary>
    /// Example validation rule that checks if a string property is not null or empty.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to validate.</typeparam>
    public abstract class NotEmptyValidationRule<TRequest> : IValidationRule<TRequest>
    {
        /// <summary>
        /// Gets the name of the property being validated.
        /// </summary>
        protected abstract string PropertyName { get; }

        /// <summary>
        /// Gets the value of the property to validate from the request.
        /// </summary>
        /// <param name="request">The request to get the property value from.</param>
        /// <returns>The value of the property to validate.</returns>
        protected abstract string GetPropertyValue(TRequest request);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var value = GetPropertyValue(request);
            if (string.IsNullOrWhiteSpace(value))
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"{PropertyName} cannot be null or empty." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}