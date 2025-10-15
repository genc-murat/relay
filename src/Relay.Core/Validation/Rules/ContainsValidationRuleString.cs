using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string contains a specified substring.
    /// </summary>
    public class ContainsValidationRuleString : IValidationRule<string>
    {
        private readonly string _substring;
        private readonly StringComparison _comparisonType;

        /// <summary>
        /// Initializes a new instance of the ContainsValidationRuleString class.
        /// </summary>
        /// <param name="substring">The substring to search for.</param>
        /// <param name="comparisonType">The string comparison type to use.</param>
        public ContainsValidationRuleString(string substring, StringComparison comparisonType = StringComparison.Ordinal)
        {
            _substring = substring ?? throw new ArgumentNullException(nameof(substring));
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

            if (!request.Contains(_substring, _comparisonType))
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"Value must contain '{_substring}'." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}