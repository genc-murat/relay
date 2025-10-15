using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid Turkish phone number.
    /// </summary>
    public class TurkishPhoneValidationRule : IValidationRule<string>
    {
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TurkishPhoneValidationRule"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public TurkishPhoneValidationRule(string errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Invalid Turkish phone number.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request) || !IsValidTurkishPhone(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        private static bool IsValidTurkishPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            // Remove all non-digit characters
            var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

            // Check for +90 prefix
            if (phone.StartsWith("+90"))
            {
                digitsOnly = digitsOnly.Substring(2);
            }
            else if (phone.StartsWith("90") && digitsOnly.Length == 12)
            {
                digitsOnly = digitsOnly.Substring(2);
            }

            // Should be 10 digits
            if (digitsOnly.Length != 10)
            {
                return false;
            }

            // Mobile numbers start with 5
            return digitsOnly.StartsWith("5");
        }
    }
}