using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid SEDOL number.
    /// </summary>
    public class SedolValidationRule : IValidationRule<string>
    {
        private readonly string _errorMessage;
        private static readonly int[] Weights = { 1, 3, 1, 7, 3, 9, 1 };

        /// <summary>
        /// Initializes a new instance of the <see cref="SedolValidationRule"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public SedolValidationRule(string? errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Invalid SEDOL number.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request) || request.Length != 7)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            var sedol = request.ToUpper();
            if (sedol.Any(c => !"0123456789BCDFGHJKLMNPQRSTVWXYZ".Contains(c)))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            var sum = 0;
            for (var i = 0; i < 6; i++)
            {
                var c = sedol[i];
                var v = char.IsDigit(c) ? c - '0' : c - 'A' + 10;
                sum += v * Weights[i];
            }

            var checkDigit = (10 - (sum % 10)) % 10;
            if (checkDigit != sedol[6] - '0')
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}