using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a numeric value represents a valid percentage.
    /// Supports both decimal and integer percentages with configurable ranges.
    /// </summary>
    public class PercentageValidationRule : IValidationRule<double>
    {
        private readonly double _minPercentage;
        private readonly double _maxPercentage;
        private readonly bool _allowNegative;

        /// <summary>
        /// Initializes a new instance of the PercentageValidationRule class.
        /// </summary>
        /// <param name="minPercentage">Minimum allowed percentage (default: 0).</param>
        /// <param name="maxPercentage">Maximum allowed percentage (default: 100).</param>
        /// <param name="allowNegative">Whether to allow negative percentages (default: false).</param>
        public PercentageValidationRule(double minPercentage = 0, double maxPercentage = 100, bool allowNegative = false)
        {
            _minPercentage = minPercentage;
            _maxPercentage = maxPercentage;
            _allowNegative = allowNegative;
        }

        /// <summary>
        /// Creates a standard percentage validation rule (0-100%).
        /// </summary>
        public static PercentageValidationRule Standard()
        {
            return new PercentageValidationRule(0, 100, false);
        }

        /// <summary>
        /// Creates a percentage validation rule allowing negative values.
        /// </summary>
        public static PercentageValidationRule WithNegative(double minPercentage = -100, double maxPercentage = 100)
        {
            return new PercentageValidationRule(minPercentage, maxPercentage, true);
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(double request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var errors = new List<string>();

            // Check for NaN or Infinity
            if (double.IsNaN(request) || double.IsInfinity(request))
            {
                errors.Add("Percentage must be a valid number.");
                return new ValueTask<IEnumerable<string>>(errors);
            }

            if (!_allowNegative && request < 0)
            {
                errors.Add("Percentage cannot be negative.");
            }

            if (request < _minPercentage)
            {
                errors.Add($"Percentage cannot be less than {_minPercentage}%.");
            }

            if (request > _maxPercentage)
            {
                errors.Add($"Percentage cannot exceed {_maxPercentage}%.");
            }

            // Check for reasonable precision (more than 2 decimal places might be suspicious)
            var decimalPlaces = GetDecimalPlaces(request);
            if (decimalPlaces > 4)
            {
                errors.Add("Percentage has too many decimal places (maximum 4 allowed).");
            }

            return new ValueTask<IEnumerable<string>>(errors);
        }

        private static int GetDecimalPlaces(double value)
        {
            var stringValue = value.ToString("G15"); // High precision string representation
            var decimalIndex = stringValue.IndexOf('.');
            return decimalIndex == -1 ? 0 : stringValue.Length - decimalIndex - 1;
        }
    }
}