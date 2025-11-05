using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string represents a valid date and time.
    /// </summary>
    public class DateTimeValidationRule : IValidationRule<string>
    {
        private readonly string[] _formats;
        private readonly IFormatProvider _formatProvider;
        private readonly DateTimeStyles _dateTimeStyles;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeValidationRule"/> class.
        /// </summary>
        /// <param name="formats">The date and time formats to try parsing with. Defaults to common formats.</param>
        /// <param name="formatProvider">The format provider to use. Defaults to InvariantCulture.</param>
        /// <param name="dateTimeStyles">The date time styles to use. Defaults to None.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public DateTimeValidationRule(
            string[]? formats = null,
            IFormatProvider? formatProvider = null,
            DateTimeStyles dateTimeStyles = DateTimeStyles.None,
            string? errorMessage = null)
        {
            _formats = formats ?? new[]
            {
                "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "MM/dd/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss"
            };
            _formatProvider = formatProvider ?? CultureInfo.InvariantCulture;
            _dateTimeStyles = dateTimeStyles;
            _errorMessage = errorMessage ?? "Invalid date and time format.";
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