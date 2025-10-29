using System;
using System.Collections.Generic;

namespace Relay.Core.ContractValidation.Models;

/// <summary>
/// Represents a validation error with detailed information.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON path to the invalid property.
    /// </summary>
    public string JsonPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected value or constraint.
    /// </summary>
    public object? ExpectedValue { get; init; }

    /// <summary>
    /// Gets or sets the actual value that failed validation.
    /// </summary>
    public object? ActualValue { get; init; }

    /// <summary>
    /// Gets or sets the schema constraint that was violated.
    /// </summary>
    public string? SchemaConstraint { get; init; }

    /// <summary>
    /// Gets or sets suggested fixes for the validation error.
    /// </summary>
    public List<string> SuggestedFixes { get; init; } = new();

    /// <summary>
    /// Gets or sets the severity level of the validation error.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Creates a new validation error with the specified error code and message.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A new validation error instance.</returns>
    public static ValidationError Create(string errorCode, string message)
    {
        return new ValidationError
        {
            ErrorCode = errorCode,
            Message = message
        };
    }

    /// <summary>
    /// Creates a new validation error with the specified error code, message, and JSON path.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="jsonPath">The JSON path to the invalid property.</param>
    /// <returns>A new validation error instance.</returns>
    public static ValidationError Create(string errorCode, string message, string jsonPath)
    {
        return new ValidationError
        {
            ErrorCode = errorCode,
            Message = message,
            JsonPath = jsonPath
        };
    }

    /// <summary>
    /// Returns a string representation of the validation error.
    /// </summary>
    /// <returns>A string representation of the validation error.</returns>
    public override string ToString()
    {
        var path = string.IsNullOrEmpty(JsonPath) ? "root" : JsonPath;
        return $"[{ErrorCode}] {Message} at '{path}'";
    }
}
