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
    /// Validation rule that checks if a string represents a valid duration.
    /// Supports ISO 8601 duration format (PnYnMnDTnHnMnS) and simple formats.
    /// </summary>
    public class DurationValidationRule : IValidationRule<string>
    {
        private static readonly Regex Iso8601DurationRegex = new Regex(
            @"^P(?:\d+Y)?(?:\d+M)?(?:\d+W)?(?:\d+D)?(?:T(?:\d+H)?(?:\d+M)?(?:\d+S)?)?$",
            RegexOptions.Compiled);

        private static bool IsValidIso8601Duration(string duration)
        {
            if (!Iso8601DurationRegex.IsMatch(duration))
            {
                return false;
            }
            // Ensure it has at least one digit after P
            return duration.Length > 1 && duration.Substring(1).Any(char.IsDigit);
        }

        private static readonly Regex SimpleDurationRegex = new Regex(
            @"^(?:(\d+)\s*days?\s*)?(?:(\d+)\s*hours?\s*)?(?:(\d+)\s*minutes?\s*)?(?:(\d+)\s*seconds?\s*)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TimeSpanRegex = new Regex(
            @"^(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var duration = request.Trim();

            // Try ISO 8601 duration format
            if (IsValidIso8601Duration(duration))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            // Try TimeSpan format (d.HH:MM:SS or HH:MM:SS or MM:SS)
            if (TimeSpanRegex.IsMatch(duration))
            {
                if (TimeSpan.TryParse(duration, out _))
                {
                    return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
                }
                else
                {
                    return new ValueTask<IEnumerable<string>>(new[] {
                        "Invalid duration format. Supported formats: ISO 8601 (P1Y2M3DT4H5M6S), TimeSpan (HH:MM:SS), or simple text (1 day 2 hours 30 minutes)"
                    });
                }
            }

            // Try simple duration format
            if (SimpleDurationRegex.IsMatch(duration))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }



            return new ValueTask<IEnumerable<string>>(new[] {
                "Invalid duration format. Supported formats: ISO 8601 (P1Y2M3DT4H5M6S), TimeSpan (HH:MM:SS), or simple text (1 day 2 hours 30 minutes)"
            });
        }
    }
}