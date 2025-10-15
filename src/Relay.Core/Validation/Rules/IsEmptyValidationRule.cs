using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is empty (but not null).
    /// </summary>
    public class IsEmptyValidationRule : IValidationRule<string>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (request.Length != 0)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Value must be empty." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}