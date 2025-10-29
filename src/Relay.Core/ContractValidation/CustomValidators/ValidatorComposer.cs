using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.CustomValidators;

/// <summary>
/// Composes multiple custom validators and orchestrates their execution.
/// </summary>
public sealed class ValidatorComposer
{
    private readonly List<ICustomValidator> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorComposer"/> class.
    /// </summary>
    /// <param name="validators">The collection of custom validators to compose.</param>
    public ValidatorComposer(IEnumerable<ICustomValidator> validators)
    {
        _validators = validators
            .OrderByDescending(v => v.Priority)
            .ToList();
    }

    /// <summary>
    /// Gets the number of validators in the composer.
    /// </summary>
    public int ValidatorCount => _validators.Count;

    /// <summary>
    /// Validates the object using all applicable validators.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A validation result containing all errors from applicable validators.</returns>
    public async ValueTask<ValidationResult> ValidateAsync(
        object obj,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<ValidationError>();

        var applicableValidators = _validators
            .Where(v => v.AppliesTo(context.ObjectType))
            .ToList();

        if (applicableValidators.Count == 0)
        {
            return new ValidationResult
            {
                IsValid = true,
                ValidatorName = nameof(ValidatorComposer),
                ValidationDuration = stopwatch.Elapsed
            };
        }

        foreach (var validator in applicableValidators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var validatorErrors = await validator.ValidateAsync(obj, context, cancellationToken);
                errors.AddRange(validatorErrors);
            }
            catch (Exception ex)
            {
                // If a validator throws an exception, wrap it as a validation error
                errors.Add(ValidationError.Create(
                    ValidationErrorCodes.CustomValidationFailed,
                    $"Validator '{validator.GetType().Name}' threw an exception: {ex.Message}"));
            }
        }

        stopwatch.Stop();

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            ValidatorName = nameof(ValidatorComposer),
            ValidationDuration = stopwatch.Elapsed
        };
    }

    /// <summary>
    /// Gets all validators that apply to the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>A collection of validators that apply to the type.</returns>
    public IEnumerable<ICustomValidator> GetApplicableValidators(Type type)
    {
        return _validators.Where(v => v.AppliesTo(type));
    }
}
