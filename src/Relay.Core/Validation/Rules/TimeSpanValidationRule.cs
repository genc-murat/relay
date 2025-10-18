using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a TimeSpan is within a specified range.
    /// </summary>
    public class TimeSpanValidationRule : IValidationRule<TimeSpan>
    {
        private readonly TimeSpan? _minValue;
        private readonly TimeSpan? _maxValue;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanValidationRule"/> class.
        /// </summary>
        /// <param name="minValue">The minimum allowed TimeSpan value (inclusive).</param>
        /// <param name="maxValue">The maximum allowed TimeSpan value (inclusive).</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public TimeSpanValidationRule(TimeSpan? minValue = null, TimeSpan? maxValue = null, string? errorMessage = null)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _errorMessage = errorMessage ?? BuildDefaultErrorMessage();
        }

        private string BuildDefaultErrorMessage()
        {
            if (_minValue.HasValue && _maxValue.HasValue)
            {
                return $"Timespan must be between {_minValue.Value} and {_maxValue.Value}.";
            }
            if (_minValue.HasValue)
            {
                return $"Timespan must be greater than or equal to {_minValue.Value}.";
            }
            if (_maxValue.HasValue)
            {
                return $"Timespan must be less than or equal to {_maxValue.Value}.";
            }
            return "Invalid TimeSpan.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(TimeSpan request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var errors = new List<string>();

            if (_minValue.HasValue && request < _minValue.Value)
            {
                errors.Add(_errorMessage);
            }

            if (_maxValue.HasValue && request > _maxValue.Value)
            {
                errors.Add(_errorMessage);
            }

            return new ValueTask<IEnumerable<string>>(errors);
        }
    }
}