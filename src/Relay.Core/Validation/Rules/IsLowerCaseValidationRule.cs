using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string consists only of lowercase letters.
    /// </summary>
    public class IsLowerCaseValidationRule : IValidationRule<string>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!request.All(char.IsLower))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Value must consist only of lowercase letters." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}