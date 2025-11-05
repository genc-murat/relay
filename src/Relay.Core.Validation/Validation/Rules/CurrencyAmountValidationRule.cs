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
    /// Validation rule that checks if a string represents a valid currency amount.
    /// Supports various currency formats with configurable decimal places.
    /// </summary>
    public class CurrencyAmountValidationRule : IValidationRule<string>
    {
        private static readonly Regex CurrencyRegex = new Regex(
            @"^-?\d{1,15}(?:\.\d{1,8})?$",
            RegexOptions.Compiled);

        private readonly int _maxDecimalPlaces;
        private readonly decimal _minAmount;
        private readonly decimal _maxAmount;
        private readonly bool _allowNegative;

        /// <summary>
        /// Initializes a new instance of the CurrencyAmountValidationRule class.
        /// </summary>
        /// <param name="maxDecimalPlaces">Maximum allowed decimal places (default: 2).</param>
        /// <param name="minAmount">Minimum allowed amount (default: 0).</param>
        /// <param name="maxAmount">Maximum allowed amount (default: 999999999999.99).</param>
        /// <param name="allowNegative">Whether to allow negative amounts (default: true).</param>
        public CurrencyAmountValidationRule(
            int maxDecimalPlaces = 2,
            decimal minAmount = 0,
            decimal maxAmount = 999999999999.99M,
            bool allowNegative = true)
        {
            _maxDecimalPlaces = Math.Max(0, Math.Min(maxDecimalPlaces, 8)); // Limit to 0-8 decimal places
            _minAmount = minAmount;
            _maxAmount = maxAmount;
            _allowNegative = allowNegative;
        }

        /// <summary>
        /// Creates a standard currency validation rule (2 decimal places, positive only).
        /// </summary>
        public static CurrencyAmountValidationRule Standard()
        {
            return new CurrencyAmountValidationRule(2, 0, 999999999999.99M, false);
        }

        /// <summary>
        /// Creates a currency validation rule for cryptocurrency (up to 8 decimal places).
        /// </summary>
        public static CurrencyAmountValidationRule Crypto()
        {
            return new CurrencyAmountValidationRule(8, 0, 999999999999.99999999M, false);
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var amount = request.Trim();

            // Check basic format
            if (!CurrencyRegex.IsMatch(amount))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid currency amount format." });
            }

            // Parse the amount
            if (!decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid currency amount." });
            }

            var errors = new List<string>();

            // Check decimal places
            var decimalPlaces = GetDecimalPlaces(amount);
            if (decimalPlaces > _maxDecimalPlaces)
            {
                errors.Add($"Currency amount cannot have more than {_maxDecimalPlaces} decimal places.");
            }

            // Check negative values
            if (!_allowNegative && parsedAmount < 0)
            {
                errors.Add("Currency amount cannot be negative.");
            }

            // Check range
            if (parsedAmount < _minAmount && parsedAmount >= 0)
            {
                errors.Add($"Currency amount cannot be less than {_minAmount:C}.");
            }

            if (parsedAmount > _maxAmount && parsedAmount >= 0)
            {
                errors.Add($"Currency amount cannot exceed {_maxAmount:C}.");
            }

            // Check for leading zeros (except for decimal part)
            if (amount.StartsWith("0") && amount.Length > 1 && amount[1] != '.')
            {
                errors.Add("Currency amount cannot have leading zeros.");
            }

            return new ValueTask<IEnumerable<string>>(errors);
        }

        private static int GetDecimalPlaces(string amount)
        {
            var decimalIndex = amount.IndexOf('.');
            return decimalIndex == -1 ? 0 : amount.Length - decimalIndex - 1;
        }
    }
}