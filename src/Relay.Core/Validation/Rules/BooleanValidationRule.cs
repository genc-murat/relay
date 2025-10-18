using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a boolean value is the expected value.
    /// </summary>
    public class BooleanValidationRule : IValidationRule<bool>
    {
        private readonly bool _expectedValue;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanValidationRule"/> class.
        /// </summary>
        /// <param name="expectedValue">The expected boolean value.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public BooleanValidationRule(bool expectedValue, string? errorMessage = null)
        {
            _expectedValue = expectedValue;
            _errorMessage = errorMessage ?? $"Value must be {_expectedValue}.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(bool request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request != _expectedValue)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}