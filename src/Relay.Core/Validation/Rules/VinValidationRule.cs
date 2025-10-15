using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid Vehicle Identification Number (VIN).
    /// </summary>
    public class VinValidationRule : IValidationRule<string>
    {
        private static readonly char[] ValidCharacters = "0123456789ABCDEFGHJKLMNPRSTUVWXYZ".ToCharArray();
        private static readonly int[] Weights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };
        private static readonly int[] Values = {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9,  // 0-9
            1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 0, 7, 0, 9,  // A-R (I=9, O=0, Q=0)
            2, 3, 4, 5, 6, 7, 8, 9  // S-Z
        };

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (request.Length != 17)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "VIN must be exactly 17 characters long." });
            }

            var vin = request.ToUpperInvariant();

            // Check for valid characters
            foreach (var c in vin)
            {
                if (Array.IndexOf(ValidCharacters, c) == -1)
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "VIN contains invalid characters." });
                }
            }

            // Calculate check digit
            var sum = 0;
            for (var i = 0; i < 17; i++)
            {
                if (i == 8) continue; // Skip check digit position

                var charIndex = Array.IndexOf(ValidCharacters, vin[i]);
                sum += Values[charIndex] * Weights[i];
            }

            var checkDigit = sum % 11;
            var expectedCheckDigit = checkDigit == 10 ? 'X' : checkDigit.ToString()[0];

            if (vin[8] != expectedCheckDigit)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid VIN check digit." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}