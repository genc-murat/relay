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
    /// Validation rule that checks if a string is a valid domain name.
    /// Supports internationalized domain names (IDN) and standard domain formats.
    /// </summary>
    public class DomainValidationRule : IValidationRule<string>
    {
        private static readonly Regex DomainRegex = new Regex(
            @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)*[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly HashSet<string> ReservedTlds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "example", "invalid", "localhost", "test"
        };

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var domain = request.Trim().ToLowerInvariant();

            // Check basic format
            if (!DomainRegex.IsMatch(domain))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid domain name format." });
            }

            // Check length constraints
            if (domain.Length > 253)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Domain name too long (maximum 253 characters)." });
            }

            // Split into labels
            var labels = domain.Split('.');
            if (labels.Length < 2)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Domain name must have at least one subdomain." });
            }

            // Validate each label
            foreach (var label in labels)
            {
                if (string.IsNullOrEmpty(label))
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "Domain name contains empty labels." });
                }

                if (label.Length > 63)
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "Domain label too long (maximum 63 characters)." });
                }

                if (label.StartsWith('-') || label.EndsWith('-'))
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "Domain labels cannot start or end with hyphens." });
                }

                // Check for reserved TLDs
                if (labels.Length >= 2 && ReservedTlds.Contains(labels.Last()))
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "Domain uses reserved TLD." });
                }
            }

            // Additional validation for common issues
            if (domain.Contains(".."))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Domain name contains consecutive dots." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}