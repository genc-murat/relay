using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid MAC address.
    /// Supports formats: XX:XX:XX:XX:XX:XX, XX-XX-XX-XX-XX-XX, XXXXXXXXXXXX
    /// </summary>
    public class MacAddressValidationRule : IValidationRule<string>
    {
        private static readonly Regex MacAddressRegex = new Regex(
            @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$|^([0-9A-Fa-f]{12})$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!MacAddressRegex.IsMatch(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid MAC address format. Use XX:XX:XX:XX:XX:XX, XX-XX-XX-XX-XX-XX, or XXXXXXXXXXXX." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}