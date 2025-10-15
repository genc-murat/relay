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
    /// Validation rule that checks if a string represents valid geographic coordinates.
    /// Supports latitude/longitude in decimal degrees format.
    /// </summary>
    public class CoordinateValidationRule : IValidationRule<string>
    {
        private static readonly Regex CoordinateRegex = new Regex(
            @"^-?\d+(?:\.\d*)?,\s*-?\d+(?:\.\d*)?$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var coordinate = request.Trim();

            if (!CoordinateRegex.IsMatch(coordinate))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid coordinate format. Expected: latitude,longitude" });
            }

            // Additional validation of ranges
            var parts = coordinate.Split(',');
            if (parts.Length != 2)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid coordinate format. Expected: latitude,longitude" });
            }

            if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid coordinate values. Must be numeric." });
            }

            // Validate ranges
            if (latitude < -90 || latitude > 90)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Latitude must be between -90 and 90 degrees." });
            }

            if (longitude < -180 || longitude > 180)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Longitude must be between -180 and 180 degrees." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}