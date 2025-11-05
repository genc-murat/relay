using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string contains at least one digit.
    /// </summary>
    public class HasDigitsValidationRule : IValidationRule<string>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!request.Any(char.IsDigit))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Value must contain at least one digit." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}