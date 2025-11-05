using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a value equals a specified value.
    /// </summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    public class IsEqualValidationRule<T> : IValidationRule<T>
    {
        private readonly T _expectedValue;

        /// <summary>
        /// Initializes a new instance of the IsEqualValidationRule class.
        /// </summary>
        /// <param name="expectedValue">The expected value.</param>
        public IsEqualValidationRule(T expectedValue)
        {
            _expectedValue = expectedValue;
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (EqualityComparer<T>.Default.Equals(request, _expectedValue))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            return new ValueTask<IEnumerable<string>>(new[] { $"Value must equal '{_expectedValue}'." });
        }
    }
}