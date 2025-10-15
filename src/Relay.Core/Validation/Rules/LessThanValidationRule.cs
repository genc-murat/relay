using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Helpers;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a number is less than a specified value.
    /// </summary>
    /// <typeparam name="T">The type of number to validate (must implement IComparable).</typeparam>
    public class LessThanValidationRule<T> : IValidationRule<T> where T : IComparable<T>
    {
        private readonly T _maxValue;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="LessThanValidationRule{T}"/> class.
        /// </summary>
        /// <param name="maxValue">The maximum allowed value (exclusive).</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public LessThanValidationRule(T maxValue, string errorMessage = null)
        {
            _maxValue = maxValue;
            _errorMessage = errorMessage ?? $"Value must be less than {_maxValue}.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!GeneralValidationHelpers.IsLessThan(request, _maxValue))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}