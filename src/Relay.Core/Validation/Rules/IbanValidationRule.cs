using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid International Bank Account Number (IBAN).
    /// </summary>
    public class IbanValidationRule : IValidationRule<string>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var iban = request.Replace(" ", "").ToUpperInvariant();

            if (iban.Length < 15 || iban.Length > 34)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "IBAN length is invalid." });
            }

            // Check country code
            if (!char.IsLetter(iban[0]) || !char.IsLetter(iban[1]))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "IBAN must start with a valid country code." });
            }

            // Check check digits
            if (!char.IsDigit(iban[2]) || !char.IsDigit(iban[3]))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "IBAN check digits are invalid." });
            }

            // Move first 4 characters to the end
            var rearranged = iban.Substring(4) + iban.Substring(0, 4);

            // Convert letters to numbers (A=10, B=11, etc.)
            var numericString = ConvertToNumericString(rearranged);

            // Calculate modulo 97
            if (!IsValidModulo97(numericString))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "IBAN checksum is invalid." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        private static string ConvertToNumericString(string input)
        {
            var result = new StringBuilder();

            foreach (var c in input)
            {
                if (char.IsDigit(c))
                {
                    result.Append(c);
                }
                else if (char.IsLetter(c))
                {
                    var value = c - 'A' + 10;
                    result.Append(value);
                }
            }

            return result.ToString();
        }

        private static bool IsValidModulo97(string numericString)
        {
            // Use BigInteger for large numbers, but since we can't assume it's available,
            // we'll use a manual modulo calculation
            const int mod = 97;
            var remainder = 0;

            foreach (var c in numericString)
            {
                remainder = (remainder * 10 + (c - '0')) % mod;
            }

            return remainder == 1;
        }
    }
}