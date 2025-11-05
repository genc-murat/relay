using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Helpers;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a number is greater than a specified value.
    /// </summary>
    /// <typeparam name="T">The type of number to validate (must implement IComparable).</typeparam>
    public class GreaterThanValidationRule<T> : IValidationRule<T> where T : IComparable<T>
    {
        private readonly T _minValue;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="GreaterThanValidationRule{T}"/> class.
        /// </summary>
        /// <param name="minValue">The minimum allowed value (exclusive).</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public GreaterThanValidationRule(T minValue, string? errorMessage = null)
        {
            _minValue = minValue;
            _errorMessage = errorMessage ?? $"Value must be greater than {_minValue}.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!GeneralValidationHelpers.IsGreaterThan(request, _minValue))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}