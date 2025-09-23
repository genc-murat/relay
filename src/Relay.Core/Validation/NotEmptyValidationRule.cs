using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation
{
    /// <summary>
    /// Example validation rule that checks if a string property is not null or empty.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to validate.</typeparam>
    public abstract class NotEmptyValidationRule<TRequest> : IValidationRule<TRequest>
    {
        /// <summary>
        /// Gets the value of the property to validate from the request.
        /// </summary>
        /// <param name="request">The request to get the property value from.</param>
        /// <returns>The value of the property to validate.</returns>
        protected abstract string GetPropertyValue(TRequest request);

        /// <summary>
        /// Gets the name of the property being validated.
        /// </summary>
        protected abstract string PropertyName { get; }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            var value = GetPropertyValue(request);

            if (string.IsNullOrEmpty(value))
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"{PropertyName} cannot be null or empty." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}