using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.CustomValidators;

/// <summary>
/// Base class for custom validators providing common functionality.
/// </summary>
public abstract class AbstractCustomValidator : ICustomValidator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractCustomValidator"/> class.
    /// </summary>
    /// <param name="priority">The priority of this validator. Higher values execute first.</param>
    protected AbstractCustomValidator(int priority = 0)
    {
        Priority = priority;
    }

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public abstract bool AppliesTo(Type type);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<ValidationError>> ValidateAsync(
        object obj,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        if (obj == null)
        {
            return new[] { CreateNullObjectError(context) };
        }

        try
        {
            return await ValidateCoreAsync(obj, context, cancellationToken);
        }
        catch (Exception ex)
        {
            return new[] { CreateValidationExceptionError(ex, context) };
        }
    }

    /// <summary>
    /// Performs the core validation logic. Override this method to implement custom validation.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of validation errors, or an empty collection if validation succeeds.</returns>
    protected abstract ValueTask<IEnumerable<ValidationError>> ValidateCoreAsync(
        object obj,
        ValidationContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a validation error for a null object.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>A validation error indicating the object is null.</returns>
    protected virtual ValidationError CreateNullObjectError(ValidationContext context)
    {
        return ValidationError.Create(
            ValidationErrorCodes.CustomValidationFailed,
            $"Object of type '{context.ObjectType.Name}' cannot be null for validation.");
    }

    /// <summary>
    /// Creates a validation error for an exception that occurred during validation.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>A validation error indicating a validation exception occurred.</returns>
    protected virtual ValidationError CreateValidationExceptionError(Exception exception, ValidationContext context)
    {
        return ValidationError.Create(
            ValidationErrorCodes.CustomValidationFailed,
            $"Custom validation failed for type '{context.ObjectType.Name}': {exception.Message}");
    }

    /// <summary>
    /// Creates a validation error with the specified code and message.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A validation error.</returns>
    protected ValidationError CreateError(string errorCode, string message)
    {
        return ValidationError.Create(errorCode, message);
    }

    /// <summary>
    /// Creates a validation error with the specified code, message, and JSON path.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="jsonPath">The JSON path to the invalid property.</param>
    /// <returns>A validation error.</returns>
    protected ValidationError CreateError(string errorCode, string message, string jsonPath)
    {
        return ValidationError.Create(errorCode, message, jsonPath);
    }
}
