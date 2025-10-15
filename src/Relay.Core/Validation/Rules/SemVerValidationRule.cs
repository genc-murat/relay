using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid Semantic Version (SemVer).
    /// Supports SemVer 2.0.0 specification.
    /// </summary>
    public class SemVerValidationRule : IValidationRule<string>
    {
        // SemVer regex pattern: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
        private static readonly Regex SemVerRegex = new Regex(
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!SemVerRegex.IsMatch(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid Semantic Version format." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}