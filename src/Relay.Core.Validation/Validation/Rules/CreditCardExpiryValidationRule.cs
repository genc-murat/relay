using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid credit card expiry date.
    /// </summary>
    public class CreditCardExpiryValidationRule : IValidationRule<string>
    {
        private static readonly Regex ExpiryRegex = new Regex(@"^(0[1-9]|1[0-2])\/?([0-9]{4}|[0-9]{2})$");
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreditCardExpiryValidationRule"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public CreditCardExpiryValidationRule(string? errorMessage = null)
        {
            _errorMessage = errorMessage ?? "Invalid credit card expiry date.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            var match = ExpiryRegex.Match(request);
            if (!match.Success)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            var month = int.Parse(match.Groups[1].Value);
            var yearStr = match.Groups[2].Value;
            var year = int.Parse(yearStr);

            if (yearStr.Length == 2)
            {
                year += 2000;
            }

            var lastDayOfMonth = DateTime.DaysInMonth(year, month);
            var expiryDate = new DateTime(year, month, lastDayOfMonth);

            if (expiryDate < DateTime.Now)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Credit card has expired." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}