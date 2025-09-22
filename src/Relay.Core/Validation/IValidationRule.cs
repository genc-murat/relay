using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation
{
    /// <summary>
    /// Interface for validation rules that can be applied to requests.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to validate.</typeparam>
    public interface IValidationRule<in TRequest>
    {
        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A collection of validation errors, empty if valid.</returns>
        ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for a validator that can validate requests using multiple rules.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to validate.</typeparam>
    public interface IValidator<in TRequest>
    {
        /// <summary>
        /// Validates the request using all registered validation rules.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A collection of validation errors, empty if valid.</returns>
        ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}