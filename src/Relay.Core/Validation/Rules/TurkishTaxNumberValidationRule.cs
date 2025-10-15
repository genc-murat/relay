using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid Turkish tax number (Vergi No).
    /// </summary>
    public class TurkishTaxNumberValidationRule : IValidationRule<string>
    {
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TurkishTaxNumberValidationRule"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public TurkishTaxNumberValidationRule(string errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Invalid Turkish tax number.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request) || !IsValidTurkishTaxNumber(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        private static bool IsValidTurkishTaxNumber(string taxNumber)
        {
            if (string.IsNullOrWhiteSpace(taxNumber))
            {
                return false;
            }

            // Remove spaces
            var cleanNumber = taxNumber.Replace(" ", "");

            // Should be exactly 10 digits
            if (cleanNumber.Length != 10 || !cleanNumber.All(char.IsDigit))
            {
                return false;
            }

            // Basic checksum: sum of first 9 digits % 10 should equal 10th digit
            var digits = cleanNumber.Select(c => c - '0').ToArray();
            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += digits[i];
            }

            return sum % 10 == digits[9];
        }
    }
}