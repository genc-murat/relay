using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string has a maximum length.
    /// </summary>
    public class MaxLengthValidationRule : IValidationRule<string>
    {
        private readonly int _maxLength;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxLengthValidationRule"/> class.
        /// </summary>
        /// <param name="maxLength">The maximum allowed length (inclusive). Default is 255.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public MaxLengthValidationRule(int maxLength = 255, string errorMessage = null)
        {
            _maxLength = maxLength;
            _errorMessage = errorMessage ?? $"Length must not exceed {_maxLength} characters.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (request.Length > _maxLength)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}