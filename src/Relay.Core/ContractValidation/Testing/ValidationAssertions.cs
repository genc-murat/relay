using System;
using System.Linq;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.Testing;

/// <summary>
/// Provides assertion helper methods for validation testing.
/// </summary>
public static class ValidationAssertions
{
    /// <summary>
    /// Asserts that the validation result contains an error with the specified error code.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <param name="errorCode">The expected error code.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveError(this ValidationResult result, string errorCode)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (string.IsNullOrEmpty(errorCode))
            throw new ArgumentException("Error code cannot be null or empty", nameof(errorCode));

        if (!result.Errors.Any(e => e.ErrorCode == errorCode))
        {
            var actualCodes = string.Join(", ", result.Errors.Select(e => e.ErrorCode));
            throw new ValidationAssertionException(
                $"Expected validation result to contain error code '{errorCode}', but found: [{actualCodes}]");
        }
    }

    /// <summary>
    /// Asserts that the validation result contains an error at the specified JSON path.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <param name="jsonPath">The expected JSON path.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveErrorAtPath(this ValidationResult result, string jsonPath)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (jsonPath == null)
            throw new ArgumentNullException(nameof(jsonPath));

        if (!result.Errors.Any(e => e.JsonPath == jsonPath))
        {
            var actualPaths = string.Join(", ", result.Errors.Select(e => $"'{e.JsonPath}'"));
            throw new ValidationAssertionException(
                $"Expected validation result to contain error at path '{jsonPath}', but found errors at: [{actualPaths}]");
        }
    }

    /// <summary>
    /// Asserts that the validation error contains the specified suggested fix.
    /// </summary>
    /// <param name="error">The validation error to check.</param>
    /// <param name="suggestion">The expected suggestion.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldSuggestFix(this ValidationError error, string suggestion)
    {
        if (error == null)
            throw new ArgumentNullException(nameof(error));

        if (string.IsNullOrEmpty(suggestion))
            throw new ArgumentException("Suggestion cannot be null or empty", nameof(suggestion));

        if (!error.SuggestedFixes.Any(s => s.Contains(suggestion, StringComparison.OrdinalIgnoreCase)))
        {
            var actualSuggestions = string.Join(", ", error.SuggestedFixes.Select(s => $"'{s}'"));
            throw new ValidationAssertionException(
                $"Expected validation error to suggest fix containing '{suggestion}', but found: [{actualSuggestions}]");
        }
    }

    /// <summary>
    /// Asserts that the validation result is valid (no errors).
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldBeValid(this ValidationResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (!result.IsValid)
        {
            var errorMessages = string.Join(Environment.NewLine, result.Errors.Select(e => $"  - {e.ErrorCode}: {e.Message}"));
            throw new ValidationAssertionException(
                $"Expected validation result to be valid, but found {result.Errors.Count} error(s):{Environment.NewLine}{errorMessages}");
        }
    }

    /// <summary>
    /// Asserts that the validation result is invalid (has errors).
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldBeInvalid(this ValidationResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (result.IsValid)
        {
            throw new ValidationAssertionException(
                "Expected validation result to be invalid, but it was valid");
        }
    }

    /// <summary>
    /// Asserts that the validation result contains exactly the specified number of errors.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <param name="expectedCount">The expected number of errors.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveErrorCount(this ValidationResult result, int expectedCount)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (expectedCount < 0)
            throw new ArgumentException("Expected count cannot be negative", nameof(expectedCount));

        if (result.Errors.Count != expectedCount)
        {
            throw new ValidationAssertionException(
                $"Expected validation result to have {expectedCount} error(s), but found {result.Errors.Count}");
        }
    }

    /// <summary>
    /// Asserts that the validation result contains an error with the specified message.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <param name="message">The expected error message (case-insensitive partial match).</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveErrorMessage(this ValidationResult result, string message)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        if (!result.Errors.Any(e => e.Message.Contains(message, StringComparison.OrdinalIgnoreCase)))
        {
            var actualMessages = string.Join(Environment.NewLine, result.Errors.Select(e => $"  - {e.Message}"));
            throw new ValidationAssertionException(
                $"Expected validation result to contain error message with '{message}', but found:{Environment.NewLine}{actualMessages}");
        }
    }

    /// <summary>
    /// Asserts that the validation error has the specified severity.
    /// </summary>
    /// <param name="error">The validation error to check.</param>
    /// <param name="severity">The expected severity.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveSeverity(this ValidationError error, ValidationSeverity severity)
    {
        if (error == null)
            throw new ArgumentNullException(nameof(error));

        if (error.Severity != severity)
        {
            throw new ValidationAssertionException(
                $"Expected validation error to have severity '{severity}', but found '{error.Severity}'");
        }
    }

    /// <summary>
    /// Asserts that the validation result completed within the specified duration.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <param name="maxDuration">The maximum expected duration.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldCompleteWithin(this ValidationResult result, TimeSpan maxDuration)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (result.ValidationDuration > maxDuration)
        {
            throw new ValidationAssertionException(
                $"Expected validation to complete within {maxDuration.TotalMilliseconds}ms, but took {result.ValidationDuration.TotalMilliseconds}ms");
        }
    }

    /// <summary>
    /// Asserts that the validation error has expected and actual values set.
    /// </summary>
    /// <param name="error">The validation error to check.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveExpectedAndActualValues(this ValidationError error)
    {
        if (error == null)
            throw new ArgumentNullException(nameof(error));

        if (error.ExpectedValue == null && error.ActualValue == null)
        {
            throw new ValidationAssertionException(
                "Expected validation error to have ExpectedValue or ActualValue set, but both were null");
        }
    }

    /// <summary>
    /// Asserts that the validation result has a validator name set.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveValidatorName(this ValidationResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (string.IsNullOrEmpty(result.ValidatorName))
        {
            throw new ValidationAssertionException(
                "Expected validation result to have a ValidatorName set, but it was null or empty");
        }
    }

    /// <summary>
    /// Asserts that the validation result has the specified validator name.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <param name="validatorName">The expected validator name.</param>
    /// <exception cref="ValidationAssertionException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveValidatorName(this ValidationResult result, string validatorName)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (string.IsNullOrEmpty(validatorName))
            throw new ArgumentException("Validator name cannot be null or empty", nameof(validatorName));

        if (result.ValidatorName != validatorName)
        {
            throw new ValidationAssertionException(
                $"Expected validation result to have validator name '{validatorName}', but found '{result.ValidatorName}'");
        }
    }
}

/// <summary>
/// Exception thrown when a validation assertion fails.
/// </summary>
public sealed class ValidationAssertionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ValidationAssertionException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ValidationAssertionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ValidationAssertionException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ValidationAssertionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
