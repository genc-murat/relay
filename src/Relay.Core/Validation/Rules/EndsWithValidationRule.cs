using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string ends with a specified suffix.
    /// </summary>
    public class EndsWithValidationRule : IValidationRule<string>
    {
        private readonly string _suffix;
        private readonly StringComparison _comparisonType;

        /// <summary>
        /// Initializes a new instance of the EndsWithValidationRule class.
        /// </summary>
        /// <param name="suffix">The suffix to check for.</param>
        /// <param name="comparisonType">The string comparison type to use.</param>
        public EndsWithValidationRule(string suffix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            _suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
            _comparisonType = comparisonType;
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!request.EndsWith(_suffix, _comparisonType))
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"Value must end with '{_suffix}'." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}