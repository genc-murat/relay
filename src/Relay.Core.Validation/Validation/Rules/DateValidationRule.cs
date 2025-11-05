using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string represents a valid date.
    /// </summary>
    public class DateValidationRule : IValidationRule<string>
    {
        private readonly string[] _formats;
        private readonly IFormatProvider _formatProvider;
        private readonly DateTimeStyles _dateTimeStyles;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateValidationRule"/> class.
        /// </summary>
        /// <param name="formats">The date formats to try parsing with. Defaults to common formats.</param>
        /// <param name="formatProvider">The format provider to use. Defaults to InvariantCulture.</param>
        /// <param name="dateTimeStyles">The date time styles to use. Defaults to None.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public DateValidationRule(
            string[]? formats = null,
            IFormatProvider? formatProvider = null,
            DateTimeStyles dateTimeStyles = DateTimeStyles.None,
            string? errorMessage = null)
        {
            _formats = formats ?? new[] { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd" };
            _formatProvider = formatProvider ?? CultureInfo.InvariantCulture;
            _dateTimeStyles = dateTimeStyles;
            _errorMessage = errorMessage ?? "Invalid date format.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (DateTime.TryParseExact(request, _formats, _formatProvider, _dateTimeStyles, out _))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
        }
    }
}