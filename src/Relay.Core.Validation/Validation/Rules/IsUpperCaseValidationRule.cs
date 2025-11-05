using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string consists only of uppercase letters.
    /// </summary>
    public class IsUpperCaseValidationRule : IValidationRule<string>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!request.All(char.IsUpper))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Value must consist only of uppercase letters." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}