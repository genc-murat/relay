using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a number is within a specified range.
    /// </summary>
    /// <typeparam name="T">The type of number to validate (must implement IComparable).</typeparam>
    public class RangeValidationRule<T> : IValidationRule<T> where T : IComparable<T>
    {
        private readonly T _minValue;
        private readonly T _maxValue;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeValidationRule{T}"/> class.
        /// </summary>
        /// <param name="minValue">The minimum allowed value (inclusive).</param>
        /// <param name="maxValue">The maximum allowed value (inclusive).</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public RangeValidationRule(T minValue, T maxValue, string errorMessage = null)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _errorMessage = errorMessage ?? $"Value must be between {_minValue} and {_maxValue}.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request.CompareTo(_minValue) < 0 || request.CompareTo(_maxValue) > 0)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}