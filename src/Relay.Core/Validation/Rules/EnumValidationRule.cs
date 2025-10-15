using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string represents a valid enum value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to validate against.</typeparam>
    public class EnumValidationRule<TEnum> : IValidationRule<string> where TEnum : struct, Enum
    {
        private readonly bool _ignoreCase;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumValidationRule{TEnum}"/> class.
        /// </summary>
        /// <param name="ignoreCase">Whether to ignore case when parsing the enum value.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public EnumValidationRule(bool ignoreCase = false, string errorMessage = null)
        {
            _ignoreCase = ignoreCase;
            _errorMessage = errorMessage ?? $"Invalid value for enum {typeof(TEnum).Name}.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (Enum.TryParse<TEnum>(request, _ignoreCase, out _))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
        }
    }
}