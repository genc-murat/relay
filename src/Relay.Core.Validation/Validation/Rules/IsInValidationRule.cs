using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a value is in a specified list of allowed values.
    /// </summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    public class IsInValidationRule<T> : IValidationRule<T>
    {
        private readonly IEnumerable<T> _allowedValues;

        /// <summary>
        /// Initializes a new instance of the IsInValidationRule class.
        /// </summary>
        /// <param name="allowedValues">The list of allowed values.</param>
        public IsInValidationRule(IEnumerable<T> allowedValues)
        {
            _allowedValues = allowedValues ?? throw new ArgumentNullException(nameof(allowedValues));
        }

        /// <summary>
        /// Initializes a new instance of the IsInValidationRule class.
        /// </summary>
        /// <param name="allowedValues">The array of allowed values.</param>
        public IsInValidationRule(params T[] allowedValues)
        {
            _allowedValues = allowedValues ?? throw new ArgumentNullException(nameof(allowedValues));
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_allowedValues.Contains(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            return new ValueTask<IEnumerable<string>>(new[] { $"Value must be one of: {string.Join(", ", _allowedValues)}." });
        }
    }
}