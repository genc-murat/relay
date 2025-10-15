using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if an integer represents a valid age.
    /// </summary>
    public class AgeValidationRule : IValidationRule<int>
    {
        private readonly int _minAge;
        private readonly int _maxAge;

        /// <summary>
        /// Initializes a new instance of the AgeValidationRule class.
        /// </summary>
        /// <param name="minAge">Minimum allowed age (default: 0).</param>
        /// <param name="maxAge">Maximum allowed age (default: 150).</param>
        public AgeValidationRule(int minAge = 0, int maxAge = 150)
        {
            _minAge = minAge;
            _maxAge = maxAge;
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(int request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var errors = new List<string>();

            if (request < _minAge)
            {
                errors.Add($"Age cannot be less than {_minAge} years.");
            }

            if (request > _maxAge)
            {
                errors.Add($"Age cannot exceed {_maxAge} years.");
            }

            // Additional validation for reasonable ages
            if (request < 0)
            {
                errors.Add("Age cannot be negative.");
            }

            return new ValueTask<IEnumerable<string>>(errors);
        }
    }
}