using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a numeric value is not zero.
    /// </summary>
    /// <typeparam name="T">The numeric type to validate.</typeparam>
    public class NonZeroValidationRule<T> : IValidationRule<T> where T : struct, IComparable<T>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request.CompareTo(default) == 0)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Value must not be zero." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}