using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.ContractValidation.CustomValidators;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.Observability;

namespace Relay.Core.ContractValidation;

/// <summary>
/// Core validation engine that orchestrates schema validation and custom validators.
/// </summary>
public sealed class ValidationEngine
{
    private readonly ValidatorComposer? _validatorComposer;
    private readonly ILogger<ValidationEngine> _logger;
    private readonly ContractValidationMetrics? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationEngine"/> class.
    /// </summary>
    public ValidationEngine()
    {
        _validatorComposer = null;
        _logger = NullLogger<ValidationEngine>.Instance;
        _metrics = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationEngine"/> class with custom validators.
    /// </summary>
    /// <param name="validatorComposer">The validator composer for custom validators.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <param name="metrics">Optional metrics collector.</param>
    public ValidationEngine(
        ValidatorComposer validatorComposer,
        ILogger<ValidationEngine>? logger = null,
        ContractValidationMetrics? metrics = null)
    {
        _validatorComposer = validatorComposer ?? throw new ArgumentNullException(nameof(validatorComposer));
        _logger = logger ?? NullLogger<ValidationEngine>.Instance;
        _metrics = metrics;
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
        using var activity = ContractValidationActivitySource.Instance.StartActivity(
            "ValidationEngine.Validate",
            ActivityKind.Internal);

        activity?.SetTag("object_type", obj?.GetType().Name ?? "null");
        activity?.SetTag("is_request", context.IsRequest);

        if (obj == null)
        {
            _logger.LogWarning(
                ValidationEventIds.ValidationFailed,
                "Validation failed: Object cannot be null");

            return ValidationResult.Failure(
                ValidationErrorCodes.CustomValidationFailed,
                "Object cannot be null for validation.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug(
                ValidationEventIds.CustomValidatorStarted,
                "Starting custom validator execution for {ObjectType}",
                obj.GetType().Name);

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
                _logger.LogDebug(
                    ValidationEventIds.CustomValidatorCompleted,
                    "No custom validators configured for {ObjectType}",
                    obj.GetType().Name);

                return new ValidationResult
                {
                    IsValid = true,
                    ValidatorName = nameof(ValidationEngine),
                    ValidationDuration = stopwatch.Elapsed
                };
            }

            // Log completion
            _logger.LogInformation(
                ValidationEventIds.CustomValidatorCompleted,
                "Custom validator execution completed for {ObjectType}. IsValid: {IsValid}, ErrorCount: {ErrorCount}, Duration: {Duration}ms",
                obj.GetType().Name,
                customValidationResult.IsValid,
                customValidationResult.Errors.Count,
                stopwatch.ElapsedMilliseconds);

            // Record metrics
            _metrics?.RecordCustomValidatorExecution(
                obj.GetType().Name,
                success: true);

            activity?.SetTag("is_valid", customValidationResult.IsValid);
            activity?.SetTag("error_count", customValidationResult.Errors.Count);

            // Return custom validation result
            return new ValidationResult
            {
                IsValid = customValidationResult.IsValid,
                Errors = customValidationResult.Errors,
                ValidatorName = nameof(ValidationEngine),
                ValidationDuration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ValidationEventIds.CustomValidatorFailed,
                ex,
                "Custom validator execution failed for {ObjectType}",
                obj.GetType().Name);

            _metrics?.RecordCustomValidatorExecution(
                obj.GetType().Name,
                success: false);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Gets a value indicating whether custom validators are configured.
    /// </summary>
    public bool HasCustomValidators => _validatorComposer != null && _validatorComposer.ValidatorCount > 0;
}
