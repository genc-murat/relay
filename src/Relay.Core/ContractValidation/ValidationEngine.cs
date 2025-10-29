using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.CustomValidators;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation;

/// <summary>
/// Core validation engine that orchestrates schema validation and custom validators.
/// </summary>
public sealed class ValidationEngine
{
    private readonly ValidatorComposer? _validatorComposer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationEngine"/> class.
    /// </summary>
    public ValidationEngine()
    {
        _validatorComposer = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationEngine"/> class with custom validators.
    /// </summary>
    /// <param name="validatorComposer">The validator composer for custom validators.</param>
    public ValidationEngine(ValidatorComposer validatorComposer)
    {
        _validatorComposer = validatorComposer ?? throw new ArgumentNullException(nameof(validatorComposer));
    }

    /// <summary>
    /// Validates an object using custom validators.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A validation result containing any errors from custom validators.</returns>
    public async ValueTask<ValidationResult> ValidateAsync(
        object obj,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        if (obj == null)
        {
            return ValidationResult.Failure(
                ValidationErrorCodes.CustomValidationFailed,
                "Object cannot be null for validation.");
        }

        var stopwatch = Stopwatch.StartNew();

        // Execute custom validators if available
        ValidationResult? customValidationResult = null;
        if (_validatorComposer != null)
        {
            customValidationResult = await _validatorComposer.ValidateAsync(obj, context, cancellationToken);
        }

        stopwatch.Stop();

        // If no custom validators, return success
        if (customValidationResult == null)
        {
            return new ValidationResult
            {
                IsValid = true,
                ValidatorName = nameof(ValidationEngine),
                ValidationDuration = stopwatch.Elapsed
            };
        }

        // Return custom validation result
        return new ValidationResult
        {
            IsValid = customValidationResult.IsValid,
            Errors = customValidationResult.Errors,
            ValidatorName = nameof(ValidationEngine),
            ValidationDuration = stopwatch.Elapsed
        };
    }

    /// <summary>
    /// Gets a value indicating whether custom validators are configured.
    /// </summary>
    public bool HasCustomValidators => _validatorComposer != null && _validatorComposer.ValidatorCount > 0;
}
