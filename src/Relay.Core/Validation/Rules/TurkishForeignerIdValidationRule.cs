using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid Turkish ID number for foreigners (Yabancı TC Kimlik No).
    /// </summary>
    public class TurkishForeignerIdValidationRule : IValidationRule<string>
    {
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TurkishForeignerIdValidationRule"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public TurkishForeignerIdValidationRule(string errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Invalid Turkish foreigner ID number.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request) || !IsValidTurkishForeignerId(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        private static bool IsValidTurkishForeignerId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length != 11 || !id.All(char.IsDigit))
            {
                return false;
            }

            var digits = id.Select(c => c - '0').ToArray();

            // Must start with 99 for foreigners
            if (digits[0] != 9 || digits[1] != 9)
            {
                return false;
            }

            // Calculate checksums for foreigners
            // The algorithm is different for foreigners
            int oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
            int evenSum = digits[1] + digits[3] + digits[5] + digits[7];

            int digit10 = ((oddSum * 7) - evenSum) % 10;
            if (digit10 < 0) digit10 += 10; // Handle negative modulo

            int digit11 = (oddSum + evenSum + digit10) % 10;

            return digits[9] == digit10 && digits[10] == digit11;
        }
    }
}