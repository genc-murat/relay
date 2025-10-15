using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if all items in a collection are unique.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public class UniqueValidationRule<T> : IValidationRule<IEnumerable<T>>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(IEnumerable<T> request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var list = request.ToList();
            if (list.Count != list.Distinct().Count())
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Collection must contain unique items." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}