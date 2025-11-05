using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string has an exact length.
    /// </summary>
    public class ExactLengthValidationRule : IValidationRule<string>
    {
        private readonly int _exactLength;

        /// <summary>
        /// Initializes a new instance of the ExactLengthValidationRule class.
        /// </summary>
        /// <param name="exactLength">The exact length required.</param>
        public ExactLengthValidationRule(int exactLength)
        {
            if (exactLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exactLength), "Exact length must be non-negative.");
            }

            _exactLength = exactLength;
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (request.Length != _exactLength)
            {
                return new ValueTask<IEnumerable<string>>(new[] { $"Value must be exactly {_exactLength} characters long." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}