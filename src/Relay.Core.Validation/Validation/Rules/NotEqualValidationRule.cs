using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a value does not equal a specified value.
    /// </summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    public class NotEqualValidationRule<T> : IValidationRule<T>
    {
        private readonly T _forbiddenValue;

        /// <summary>
        /// Initializes a new instance of the NotEqualValidationRule class.
        /// </summary>
        /// <param name="forbiddenValue">The forbidden value.</param>
        public NotEqualValidationRule(T forbiddenValue)
        {
            _forbiddenValue = forbiddenValue;
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!EqualityComparer<T>.Default.Equals(request, _forbiddenValue))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            return new ValueTask<IEnumerable<string>>(new[] { $"Value must not equal '{_forbiddenValue}'." });
        }
    }
}