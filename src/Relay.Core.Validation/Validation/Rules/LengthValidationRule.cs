using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string length is within specified bounds.
    /// </summary>
    public class LengthValidationRule : IValidationRule<string>
    {
        private readonly int _minLength;
        private readonly int _maxLength;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="LengthValidationRule"/> class.
        /// </summary>
        /// <param name="minLength">The minimum allowed length (inclusive). Use 0 for no minimum.</param>
        /// <param name="maxLength">The maximum allowed length (inclusive). Use int.MaxValue for no maximum.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public LengthValidationRule(int minLength = 0, int maxLength = int.MaxValue, string? errorMessage = null)
        {
            _minLength = minLength;
            _maxLength = maxLength;
            _errorMessage = errorMessage ?? $"Length must be between {_minLength} and {_maxLength} characters.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null)
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            int length = request.Length;
            if (length < _minLength || length > _maxLength)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}