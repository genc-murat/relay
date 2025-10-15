using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid Turkish IBAN.
    /// </summary>
    public class TurkishIbanValidationRule : IValidationRule<string>
    {
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TurkishIbanValidationRule"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public TurkishIbanValidationRule(string errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Invalid Turkish IBAN.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request) || !IsValidTurkishIban(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        private static bool IsValidTurkishIban(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban))
            {
                return false;
            }

            // Remove spaces
            var cleanIban = iban.Replace(" ", "").ToUpper();

            // Turkish IBAN should start with TR and be 26 characters long
            if (!cleanIban.StartsWith("TR") || cleanIban.Length != 26)
            {
                return false;
            }

            // Check if all characters after TR are digits
            return cleanIban.Substring(2).All(char.IsDigit);
        }
    }
}