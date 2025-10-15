using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid hexadecimal color code.
    /// Supports 3-digit (#RGB), 6-digit (#RRGGBB), and 8-digit (#RRGGBBAA) formats.
    /// </summary>
    public class HexColorValidationRule : IValidationRule<string>
    {
        private static readonly Regex HexColorRegex = new Regex(
            @"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!HexColorRegex.IsMatch(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid hexadecimal color format. Use #RGB, #RRGGBB, or #RRGGBBAA." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}