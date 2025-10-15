using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid Turkish postal code.
    /// </summary>
    public class TurkishPostalCodeValidationRule : IValidationRule<string>
    {
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TurkishPostalCodeValidationRule"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public TurkishPostalCodeValidationRule(string errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Invalid Turkish postal code.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request) || !IsValidTurkishPostalCode(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        private static bool IsValidTurkishPostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
            {
                return false;
            }

            // Remove spaces and check if it's exactly 5 digits
            var cleanCode = postalCode.Replace(" ", "");
            return cleanCode.Length == 5 && cleanCode.All(char.IsDigit);
        }
    }
}