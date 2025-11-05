using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid IPv6 address.
    /// Supports compressed notation (::) and mixed IPv4-IPv6 notation.
    /// </summary>
    public class Ipv6ValidationRule : IValidationRule<string>
    {
        private static readonly Regex Ipv6Regex = new Regex(
            @"^(?:(?:(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4})|(?:(?:[0-9a-fA-F]{1,4}:){1,7}:)|(?:(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4})|(?:(?:[0-9a-fA-F]{1,4}:){1,5}(?::[0-9a-fA-F]{1,4}){1,2})|(?:(?:[0-9a-fA-F]{1,4}:){1,4}(?::[0-9a-fA-F]{1,4}){1,3})|(?:(?:[0-9a-fA-F]{1,4}:){1,3}(?::[0-9a-fA-F]{1,4}){1,4})|(?:(?:[0-9a-fA-F]{1,4}:){1,2}(?::[0-9a-fA-F]{1,4}){1,5})|(?:[0-9a-fA-F]{1,4}:(?:(?::[0-9a-fA-F]{1,4}){1,6}))|(?:(?::[0-9a-fA-F]{1,4}){1,7}:)|(?:::))$",
            RegexOptions.Compiled);

        private static readonly Regex Ipv4MappedIpv6Regex = new Regex(
            @"^(?:(?:(?:[0-9a-fA-F]{1,4}:){5}:)|(?:(?:[0-9a-fA-F]{1,4}:){1,7}:)|(?:(?:[0-9a-fA-F]{1,4}:){7,7}))(?:[0-9]{1,3}\.){3}[0-9]{1,3}$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var ipAddress = request.Trim();

            // Use .NET's IPAddress parsing for validation
            if (!IPAddress.TryParse(ipAddress, out var parsedAddress))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid IPv6 address format." });
            }

            if (parsedAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Address is not a valid IPv6 address." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}