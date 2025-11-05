using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string starts with a specified prefix.
    /// </summary>
    public class StartsWithValidationRule : IValidationRule<string>
    {
        private readonly string _prefix;
        private readonly StringComparison _comparisonType;

        /// <summary>
        /// Initializes a new instance of the StartsWithValidationRule class.
        /// </summary>
        /// <param name="prefix">The prefix to check for.</param>
        /// <param name="comparisonType">The string comparison type to use.</param>
        public StartsWithValidationRule(string prefix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
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

            if (!request.StartsWith(_prefix, _comparisonType))
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"Value must start with '{_prefix}'." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}