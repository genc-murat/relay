using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Validation;

/// <summary>
/// Base interface for all validators in the Relay source generator.
/// Follows the Interface Segregation Principle by defining a focused contract.
/// </summary>
/// <typeparam name="TInput">The type of input to validate</typeparam>
/// <typeparam name="TResult">The type of validation result</typeparam>
public interface IValidator<in TInput, out TResult>
{
    /// <summary>
    /// Validates the input and returns a validation result.
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>The validation result</returns>
    TResult Validate(TInput input);
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; } = new();

    /// <summary>
    /// Gets the collection of validation warnings.
    /// </summary>
    public List<ValidationWarning> Warnings { get; } = new();

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="location">The location of the error</param>
    /// <param name="descriptor">The diagnostic descriptor</param>
    public void AddError(string message, Location? location = null, DiagnosticDescriptor? descriptor = null)
    {
        Errors.Add(new ValidationError(message, location, descriptor));
    }

    /// <summary>
    /// Adds a warning to the validation result.
    /// </summary>
    /// <param name="message">The warning message</param>
    /// <param name="location">The location of the warning</param>
    /// <param name="descriptor">The diagnostic descriptor</param>
    public void AddWarning(string message, Location? location = null, DiagnosticDescriptor? descriptor = null)
    {
        Warnings.Add(new ValidationWarning(message, location, descriptor));
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    /// <param name="other">The other validation result to merge</param>
    public void Merge(ValidationResult other)
    {
        if (other == null) return;
        
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with an error.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="location">The location of the error</param>
    /// <param name="descriptor">The diagnostic descriptor</param>
    public static ValidationResult Failure(string message, Location? location = null, DiagnosticDescriptor? descriptor = null)
    {
        var result = new ValidationResult();
        result.AddError(message, location, descriptor);
        return result;
    }
}

/// <summary>
/// Represents a validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the location of the error in the source code.
    /// </summary>
    public Location? Location { get; }

    /// <summary>
    /// Gets the diagnostic descriptor for this error.
    /// </summary>
    public DiagnosticDescriptor? Descriptor { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="location">The location of the error</param>
    /// <param name="descriptor">The diagnostic descriptor</param>
    public ValidationError(string message, Location? location = null, DiagnosticDescriptor? descriptor = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Location = location;
        Descriptor = descriptor;
    }
}

/// <summary>
/// Represents a validation warning.
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// Gets the warning message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the location of the warning in the source code.
    /// </summary>
    public Location? Location { get; }

    /// <summary>
    /// Gets the diagnostic descriptor for this warning.
    /// </summary>
    public DiagnosticDescriptor? Descriptor { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationWarning"/> class.
    /// </summary>
    /// <param name="message">The warning message</param>
    /// <param name="location">The location of the warning</param>
    /// <param name="descriptor">The diagnostic descriptor</param>
    public ValidationWarning(string message, Location? location = null, DiagnosticDescriptor? descriptor = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Location = location;
        Descriptor = descriptor;
    }
}
