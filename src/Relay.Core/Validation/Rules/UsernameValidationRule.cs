using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid username.
    /// Supports alphanumeric characters, underscores, hyphens, and dots.
    /// </summary>
    public class UsernameValidationRule : IValidationRule<string>
    {
        private static readonly Regex UsernameRegex = new Regex(
            @"^[a-zA-Z0-9][a-zA-Z0-9._-]{0,30}[a-zA-Z0-9]$|^[a-zA-Z0-9]$",
            RegexOptions.Compiled);

        private static readonly HashSet<string> ReservedUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "admin", "administrator", "root", "system", "guest", "user", "test",
            "null", "undefined", "api", "www", "mail", "ftp", "localhost"
        };

        private static bool IsSpecialChar(char c)
        {
            return c == '.' || c == '_' || c == '-';
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var username = request.Trim();

            // Check length
            if (username.Length < 3)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Username must be at least 3 characters long." });
            }

            if (username.Length > 32)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Username cannot exceed 32 characters." });
            }

            // Check format
            if (!UsernameRegex.IsMatch(username))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Username can only contain letters, numbers, underscores, hyphens, and dots. Must start and end with alphanumeric characters." });
            }

            // Check for reserved usernames
            if (ReservedUsernames.Contains(username.ToLowerInvariant()))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "This username is reserved and cannot be used." });
            }

            // Check for consecutive special characters
            for (int i = 0; i < username.Length - 1; i++)
            {
                char current = username[i];
                char next = username[i + 1];
                if (IsSpecialChar(current) && IsSpecialChar(next))
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "Username cannot contain consecutive special characters." });
                }
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}