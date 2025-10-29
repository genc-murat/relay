using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.Strategies;

/// <summary>
/// Defines a strategy for handling validation operations and results.
/// </summary>
public interface IValidationStrategy
{
    /// <summary>
    /// Gets the name of the validation strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether validation should be performed for the given context.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>True if validation should be performed; otherwise, false.</returns>
    bool ShouldValidate(ValidationContext context);

    /// <summary>
    /// Handles the validation result according to the strategy's behavior.
    /// </summary>
    /// <param name="result">The validation result to handle.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the processed validation result.</returns>
    ValueTask<ValidationResult> HandleResultAsync(
        ValidationResult result,
        ValidationContext context,
        CancellationToken cancellationToken = default);
}
