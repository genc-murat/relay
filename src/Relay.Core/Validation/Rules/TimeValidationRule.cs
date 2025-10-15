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
    /// Validation rule that checks if a string represents a valid time.
    /// Supports 12-hour and 24-hour formats with optional seconds and AM/PM.
    /// </summary>
    public class TimeValidationRule : IValidationRule<string>
    {
        private static readonly Regex TimeRegex = new Regex(
            @"^(?:[01]?\d|2[0-3]):[0-5]\d(?::[0-5]\d)?(?:\s?[AP]M)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex Time12HourRegex = new Regex(
            @"^(?:0?[1-9]|1[0-2]):[0-5]\d(?::[0-5]\d)?\s?[AP]M$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex Time24HourRegex = new Regex(
            @"^(?:[01]?\d|2[0-3]):[0-5]\d(?::[0-5]\d)?$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var time = request.Trim();

            // Check basic format
            if (!TimeRegex.IsMatch(time))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid time format. Expected formats: HH:MM, HH:MM:SS, HH:MM AM/PM, or HH:MM:SS AM/PM" });
            }

            // Try to parse as DateTime to validate ranges
            if (!DateTime.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out var parsedTime))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid time value." });
            }

            // Additional validation for 12-hour format
            if (time.ToUpperInvariant().Contains("AM") || time.ToUpperInvariant().Contains("PM"))
            {
                if (!Time12HourRegex.IsMatch(time))
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "Invalid 12-hour time format." });
                }
            }
            else
            {
                // 24-hour format validation
                if (!Time24HourRegex.IsMatch(time))
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "Invalid 24-hour time format." });
                }
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}