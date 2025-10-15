using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if an integer value is even.
    /// </summary>
    public class EvenValidationRule : IValidationRule<int>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(int request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request % 2 != 0)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Value must be even." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }

    /// <summary>
    /// Validation rule that checks if a long value is even.
    /// </summary>
    public class EvenValidationRuleLong : IValidationRule<long>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(long request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request % 2 != 0)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Value must be even." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}