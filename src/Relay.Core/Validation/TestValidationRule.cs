using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation
{
    /// <summary>
    /// Example validation rule for testing purposes.
    /// </summary>
    public class TestValidationRule : IValidationRule<string>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Request cannot be null or empty." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}