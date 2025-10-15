using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is valid Base64 encoded data.
    /// </summary>
    public class Base64ValidationRule : IValidationRule<string>
    {
        private static readonly Regex Base64Regex = new Regex(
            @"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!Base64Regex.IsMatch(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid Base64 format." });
            }

            // Additional validation: try to decode to ensure it's valid
            try
            {
                Convert.FromBase64String(request);
            }
            catch (FormatException)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid Base64 data." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}