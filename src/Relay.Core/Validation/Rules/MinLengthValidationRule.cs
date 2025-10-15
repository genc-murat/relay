using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string has a minimum length.
    /// </summary>
    public class MinLengthValidationRule : IValidationRule<string>
    {
        private readonly int _minLength;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MinLengthValidationRule"/> class.
        /// </summary>
        /// <param name="minLength">The minimum allowed length (inclusive). Default is 1.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public MinLengthValidationRule(int minLength = 1, string errorMessage = null)
        {
            _minLength = minLength;
            _errorMessage = errorMessage ?? $"Length must be at least {_minLength} characters.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (request.Length < _minLength)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}