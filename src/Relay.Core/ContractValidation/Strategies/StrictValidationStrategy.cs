using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.Strategies;

/// <summary>
/// A validation strategy that throws exceptions on validation failures.
/// This strategy is suitable for production environments where contract violations should halt execution.
/// </summary>
public sealed class StrictValidationStrategy : IValidationStrategy
{
    /// <inheritdoc />
    public string Name => "Strict";

    /// <inheritdoc />
    public bool ShouldValidate(ValidationContext context)
    {
        // Always validate in strict mode
        return true;
    }

    /// <inheritdoc />
    public ValueTask<ValidationResult> HandleResultAsync(
        ValidationResult result,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        if (!result.IsValid && result.Errors.Any())
        {
            // Throw exception with all validation errors
            var errorMessages = result.Errors
                .Select(e => string.IsNullOrEmpty(e.JsonPath)
                    ? e.Message
                    : $"{e.JsonPath}: {e.Message}")
                .ToArray();

            throw new ContractValidationException(context.ObjectType, errorMessages);
        }

        return ValueTask.FromResult(result);
    }
}
