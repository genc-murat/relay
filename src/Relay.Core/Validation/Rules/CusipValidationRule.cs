using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid CUSIP number.
    /// </summary>
    public class CusipValidationRule : IValidationRule<string>
    {
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="CusipValidationRule"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public CusipValidationRule(string? errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Invalid CUSIP number.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request) || request.Length != 9)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            var cusip = request.ToUpper();
            var sum = 0;
            for (var i = 0; i < 8; i++)
            {
                var c = cusip[i];
                int v;
                if (c >= '0' && c <= '9')
                {
                    v = c - '0';
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    v = c - 'A' + 10;
                }
                else if (c == '*')
                {
                    v = 36;
                }
                else if (c == '@')
                {
                    v = 37;
                }
                else if (c == '#')
                {
                    v = 38;
                }
                else
                {
                    return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
                }

                if (i % 2 != 0) // Odd position (1-based index)
                {
                    v *= 2;
                }
                sum += (v / 10) + (v % 10);
            }

            var checkDigit = (10 - (sum % 10)) % 10;
            if (checkDigit != cusip[8] - '0')
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}