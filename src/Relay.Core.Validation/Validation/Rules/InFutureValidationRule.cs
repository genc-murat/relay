using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a comparable value is in the future.
    /// </summary>
    /// <typeparam name="T">The type to validate, must be comparable.</typeparam>
    public class InFutureValidationRule<T> : IValidationRule<T> where T : IComparable<T>
    {
        private readonly Func<T> _futureValueProvider;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="InFutureValidationRule{T}"/> class.
        /// </summary>
        /// <param name="futureValueProvider">A function that provides the value to compare against (e.g., () => DateTime.Now).</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public InFutureValidationRule(Func<T> futureValueProvider, string? errorMessage = null)
        {
            _futureValueProvider = futureValueProvider ?? throw new ArgumentNullException(nameof(futureValueProvider));
            _errorMessage = errorMessage ?? "Value must be in the future.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(T request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request.CompareTo(_futureValueProvider()) <= 0)
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}