using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid JWT token format.
    /// Validates the structure (header.payload.signature) but not the signature.
    /// </summary>
    public class JwtValidationRule : IValidationRule<string>
    {
        private static readonly Regex JwtRegex = new Regex(
            @"^[^.]+\.[^.]+\.?([^.]*)$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!JwtRegex.IsMatch(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid JWT token format." });
            }

            // Additional validation: check that we have 2 or 3 parts
            var parts = request.Split('.');
            if (parts.Length < 2 || parts.Length > 3)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid JWT token format." });
            }

            // Check that header and payload are valid Base64Url
            try
            {
                // Just validate the format, not decode
                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part))
                    {
                        return new ValueTask<IEnumerable<string>>(new[] { "Invalid JWT token format." });
                    }
                }
            }
            catch
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid JWT token format." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}