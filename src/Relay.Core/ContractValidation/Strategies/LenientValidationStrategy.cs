using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.Strategies;

/// <summary>
/// A validation strategy that logs warnings without throwing exceptions on validation failures.
/// This strategy is suitable for development environments where contract violations should be visible but not halt execution.
/// </summary>
public sealed class LenientValidationStrategy : IValidationStrategy
{
    private readonly ILogger<LenientValidationStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LenientValidationStrategy"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LenientValidationStrategy(ILogger<LenientValidationStrategy> logger)
    {
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "Lenient";

    /// <inheritdoc />
    public bool ShouldValidate(ValidationContext context)
    {
        // Always validate in lenient mode to capture warnings
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
            // Log warnings for each validation error without throwing
            var objectTypeName = context.ObjectType.Name;
            var validationType = context.IsRequest ? "Request" : "Response";

            _logger.LogWarning(
                "Contract validation warnings for {ValidationType} {ObjectType}: {ErrorCount} error(s) found",
                validationType,
                objectTypeName,
                result.Errors.Count);

            foreach (var error in result.Errors)
            {
                var errorLocation = string.IsNullOrEmpty(error.JsonPath) ? "root" : error.JsonPath;
                _logger.LogWarning(
                    "Validation warning [{ErrorCode}] at '{JsonPath}': {Message}",
                    error.ErrorCode,
                    errorLocation,
                    error.Message);
            }
        }

        // Return the result without throwing, allowing execution to continue
        return ValueTask.FromResult(result);
    }
}
