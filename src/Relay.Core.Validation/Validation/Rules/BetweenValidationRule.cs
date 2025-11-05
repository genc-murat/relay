using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a comparable value is between two bounds (inclusive).
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public class BetweenValidationRule<T> : IValidationRule<T> where T : IComparable<T>
    {
        private readonly T _minValue;
        private readonly T _maxValue;

        /// <summary>
        /// Initializes a new instance of the BetweenValidationRule class.
        /// </summary>
        /// <param name="minValue">The minimum value (inclusive).</param>
        /// <param name="maxValue">The maximum value (inclusive).</param>
        public BetweenValidationRule(T minValue, T maxValue)
        {
            _minValue = minValue ?? throw new ArgumentNullException(nameof(minValue));
            _maxValue = maxValue ?? throw new ArgumentNullException(nameof(maxValue));

            if (minValue.CompareTo(maxValue) > 0)
            {
                throw new ArgumentException("Minimum value must be less than or equal to maximum value.", nameof(minValue));
            }
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (request.CompareTo(_minValue) < 0 || request.CompareTo(_maxValue) > 0)
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"Value must be between {_minValue} and {_maxValue} (inclusive)." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}