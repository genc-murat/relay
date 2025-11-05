using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is valid JSON.
    /// </summary>
    public class JsonValidationRule : IValidationRule<string>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            try
            {
                JsonDocument.Parse(request);
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }
            catch (JsonException)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid JSON format." });
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid JSON format." });
            }
        }
    }
}