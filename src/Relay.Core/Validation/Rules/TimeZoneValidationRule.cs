using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid IANA time zone identifier.
    /// </summary>
    public class TimeZoneValidationRule : IValidationRule<string>
    {
        private static readonly HashSet<string> ValidTimeZoneIds;

        static TimeZoneValidationRule()
        {
            // Start with system time zones
            ValidTimeZoneIds = TimeZoneInfo.GetSystemTimeZones()
                .Select(tz => tz.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Add comprehensive list of common IANA time zones
            var commonIanaZones = new[]
            {
                // Americas
                "America/New_York", "America/Los_Angeles", "America/Chicago", "America/Denver",
                "America/Phoenix", "America/Anchorage", "Pacific/Honolulu", "America/Mexico_City",
                "America/Sao_Paulo", "America/Buenos_Aires", "America/Lima", "America/Bogota",

                // Europe
                "Europe/London", "Europe/Paris", "Europe/Berlin", "Europe/Rome", "Europe/Madrid",
                "Europe/Moscow", "Europe/Amsterdam", "Europe/Stockholm", "Europe/Prague",
                "Europe/Vienna", "Europe/Zurich", "Europe/Warsaw",

                // Asia
                "Asia/Tokyo", "Asia/Shanghai", "Asia/Kolkata", "Asia/Dubai", "Asia/Singapore",
                "Asia/Seoul", "Asia/Bangalore", "Asia/Hong_Kong", "Asia/Manila", "Asia/Jakarta",
                "Asia/Kuala_Lumpur", "Asia/Taipei",

                // Australia/Pacific
                "Australia/Sydney", "Australia/Melbourne", "Pacific/Auckland", "Australia/Perth",
                "Australia/Brisbane", "Australia/Adelaide", "Pacific/Fiji", "Pacific/Guam",

                // Africa
                "Africa/Cairo", "Africa/Johannesburg", "Africa/Lagos", "Africa/Nairobi",
                "Africa/Casablanca", "Africa/Algiers",

                // UTC and GMT (IANA identifiers)
                "UTC", "GMT", "EDT", "PDT", "CDT", "MDT"
            };

            foreach (var zone in commonIanaZones)
            {
                ValidTimeZoneIds.Add(zone);
            }
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!ValidTimeZoneIds.Contains(request.Trim()))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid IANA time zone identifier." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}