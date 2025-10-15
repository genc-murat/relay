using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a value is not in a specified list of forbidden values.
    /// </summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    public class NotInValidationRule<T> : IValidationRule<T>
    {
        private readonly IEnumerable<T> _forbiddenValues;

        /// <summary>
        /// Initializes a new instance of the NotInValidationRule class.
        /// </summary>
        /// <param name="forbiddenValues">The list of forbidden values.</param>
        public NotInValidationRule(IEnumerable<T> forbiddenValues)
        {
            _forbiddenValues = forbiddenValues ?? throw new ArgumentNullException(nameof(forbiddenValues));
        }

        /// <summary>
        /// Initializes a new instance of the NotInValidationRule class.
        /// </summary>
        /// <param name="forbiddenValues">The array of forbidden values.</param>
        public NotInValidationRule(params T[] forbiddenValues)
        {
            _forbiddenValues = forbiddenValues ?? throw new ArgumentNullException(nameof(forbiddenValues));
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_forbiddenValues.Contains(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            return new ValueTask<IEnumerable<string>>(new[] { $"Value must not be one of: {string.Join(", ", _forbiddenValues)}." });
        }
    }
}