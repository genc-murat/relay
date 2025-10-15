using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid phone number.
    /// Uses a basic regex pattern that can be customized.
    /// </summary>
    public class PhoneNumberValidationRule : IValidationRule<string>
    {
        private readonly Regex _phoneRegex;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneNumberValidationRule"/> class.
        /// </summary>
        /// <param name="pattern">The regex pattern for phone number validation. Defaults to a basic international pattern.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public PhoneNumberValidationRule(string pattern = @"^\+?[\d\s\-\(\)]{7,15}$", string errorMessage = null)
        {
            _phoneRegex = new Regex(pattern, RegexOptions.Compiled);
            _errorMessage = errorMessage ?? "Invalid phone number format.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!_phoneRegex.IsMatch(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}