using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a comparable value is in the past.
    /// </summary>
    /// <typeparam name="T">The type to validate, must be comparable.</typeparam>
    public class InPastValidationRule<T> : IValidationRule<T> where T : IComparable<T>
    {
        private readonly Func<T> _pastValueProvider;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="InPastValidationRule{T}"/> class.
        /// </summary>
        /// <param name="pastValueProvider">A function that provides the value to compare against (e.g., () => DateTime.Now).</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public InPastValidationRule(Func<T> pastValueProvider, string? errorMessage = null)
        {
            _pastValueProvider = pastValueProvider ?? throw new ArgumentNullException(nameof(pastValueProvider));
            _errorMessage = errorMessage ?? "Value must be in the past.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request.CompareTo(_pastValueProvider()) >= 0)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}