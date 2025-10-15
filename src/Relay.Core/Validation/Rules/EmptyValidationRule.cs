using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a collection is empty.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public class EmptyValidationRule<T> : IValidationRule<IEnumerable<T>>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(IEnumerable<T> request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (request.Any())
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Collection must be empty." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}