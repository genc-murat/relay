using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.ContractValidation.Models;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; init; } = new();

    /// <summary>
    /// Gets or sets the duration of the validation operation.
    /// </summary>
    public TimeSpan ValidationDuration { get; init; }

    /// <summary>
    /// Gets or sets the name of the validator that produced this result.
    /// </summary>
    public string? ValidatorName { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a successful validation result with a validator name.
    /// </summary>
    /// <param name="validatorName">The name of the validator.</param>
    /// <returns>A validation result indicating success.</returns>
    public static ValidationResult Success(string validatorName)
    {
        return new ValidationResult
        {
            IsValid = true,
            ValidatorName = validatorName
        };
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(params ValidationError[] errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors and validator name.
    /// </summary>
    /// <param name="validatorName">The name of the validator.</param>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(string validatorName, params ValidationError[] errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            ValidatorName = validatorName,
            Errors = errors.ToList()
        };
    }

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(string errorCode, string message)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                ValidationError.Create(errorCode, message)
            }
        };
    }

    /// <summary>
    /// Returns a string representation of the validation result.
    /// </summary>
    /// <returns>A string representation of the validation result.</returns>
    public override string ToString()
    {
        if (IsValid)
        {
            return "Validation succeeded";
        }

        var errorCount = Errors.Count;
        var errorSummary = errorCount == 1 ? "1 error" : $"{errorCount} errors";
        return $"Validation failed with {errorSummary}";
    }
}
