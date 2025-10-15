using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a collection has at least a minimum number of items.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public class MinCountValidationRule<T> : IValidationRule<IEnumerable<T>>
    {
        private readonly int _minCount;

        /// <summary>
        /// Initializes a new instance of the MinCountValidationRule class.
        /// </summary>
        /// <param name="minCount">The minimum number of items required.</param>
        public MinCountValidationRule(int minCount)
        {
            if (minCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minCount), "Minimum count must be non-negative.");
            }

            _minCount = minCount;
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(IEnumerable<T> request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var count = request.Count();
            if (count < _minCount)
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"Collection must contain at least {_minCount} items." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}