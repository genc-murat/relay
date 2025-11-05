using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a collection contains a specific item.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public class ContainsValidationRule<T> : IValidationRule<IEnumerable<T>>
    {
        private readonly T _item;

        /// <summary>
        /// Initializes a new instance of the ContainsValidationRule class.
        /// </summary>
        /// <param name="item">The item that must be contained in the collection.</param>
        public ContainsValidationRule(T item)
        {
            _item = item;
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(IEnumerable<T> request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!request.Contains(_item))
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"Collection must contain '{_item}'." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}