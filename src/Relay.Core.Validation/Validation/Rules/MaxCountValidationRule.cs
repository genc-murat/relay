using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a collection has at most a maximum number of items.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public class MaxCountValidationRule<T> : IValidationRule<IEnumerable<T>>
    {
        private readonly int _maxCount;

        /// <summary>
        /// Initializes a new instance of the MaxCountValidationRule class.
        /// </summary>
        /// <param name="maxCount">The maximum number of items allowed.</param>
        public MaxCountValidationRule(int maxCount)
        {
            if (maxCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Maximum count must be non-negative.");
            }

            _maxCount = maxCount;
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
            if (count > _maxCount)
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"Collection must contain at most {_maxCount} items." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}