using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid Cron expression.
    /// Supports standard Cron format with 5 or 6 fields (seconds optional).
    /// </summary>
    public class CronExpressionValidationRule : IValidationRule<string>
    {
        // Cron field patterns
        private static readonly Regex FieldRegex = new Regex(
            @"^(\*|([0-9]|[1-5][0-9])|(\*\/([0-9]|[1-5][0-9]))|(([0-9]|[1-5][0-9])-([0-9]|[1-5][0-9]))|(([0-9]|[1-5][0-9])(,([0-9]|[1-5][0-9]))*))$",
            RegexOptions.Compiled);

        private static readonly Regex SecondsFieldRegex = new Regex(
            @"^(\*|([0-5]?[0-9])|(\*\/([0-5]?[0-9]))|(([0-5]?[0-9])-([0-5]?[0-9]))|(([0-5]?[0-9])(,([0-5]?[0-9]))*))$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var fields = request.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length != 5 && fields.Length != 6)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Cron expression must have 5 or 6 fields." });
            }

            // Validate each field
            var errors = new List<string>();

            // Seconds (optional, only in 6-field expressions)
            if (fields.Length == 6)
            {
                if (!SecondsFieldRegex.IsMatch(fields[0]))
                {
                    errors.Add("Invalid seconds field in Cron expression.");
                }
            }

            // Minutes
            if (!FieldRegex.IsMatch(fields[fields.Length == 6 ? 1 : 0]))
            {
                errors.Add("Invalid minutes field in Cron expression.");
            }

            // Hours
            if (!Regex.IsMatch(fields[fields.Length == 6 ? 2 : 1], @"^(\*|([0-9]|1[0-9]|2[0-3])|(\*\/([0-9]|1[0-9]|2[0-3]))|(([0-9]|1[0-9]|2[0-3])-([0-9]|1[0-9]|2[0-3]))|(([0-9]|1[0-9]|2[0-3])(,([0-9]|1[0-9]|2[0-3]))*))$"))
            {
                errors.Add("Invalid hours field in Cron expression.");
            }

            // Day of month
            if (!Regex.IsMatch(fields[fields.Length == 6 ? 3 : 2], @"^(\*|(0?[1-9]|[12][0-9]|3[01])|(\*\/(0?[1-9]|[12][0-9]|3[01]))|((0?[1-9]|[12][0-9]|3[01])-(0?[1-9]|[12][0-9]|3[01]))|((0?[1-9]|[12][0-9]|3[01])(,(0?[1-9]|[12][0-9]|3[01]))*)|L|W)$"))
            {
                errors.Add("Invalid day of month field in Cron expression.");
            }

            // Month
            if (!Regex.IsMatch(fields[fields.Length == 6 ? 4 : 3], @"^(\*|([1-9]|1[0-2])|(\*\/([1-9]|1[0-2]))|(([1-9]|1[0-2])-([1-9]|1[0-2]))|(([1-9]|1[0-2])(,([1-9]|1[0-2]))*)|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)$", RegexOptions.IgnoreCase))
            {
                errors.Add("Invalid month field in Cron expression.");
            }

            // Day of week
            if (!Regex.IsMatch(fields[fields.Length == 6 ? 5 : 4], @"^(\*|([0-6])|(\*\/([0-6]))|(([0-6])-([0-6]))|(([0-6])(,([0-6]))*)|SUN|MON|TUE|WED|THU|FRI|SAT|L)$", RegexOptions.IgnoreCase))
            {
                errors.Add("Invalid day of week field in Cron expression.");
            }

            return new ValueTask<IEnumerable<string>>(errors);
        }
    }
}