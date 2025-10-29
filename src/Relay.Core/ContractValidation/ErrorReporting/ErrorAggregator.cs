using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.ErrorReporting;

/// <summary>
/// Aggregates and formats validation errors into structured output.
/// </summary>
public class ErrorAggregator
{
    private readonly List<ValidationError> _errors = new();
    private readonly int _maxErrorCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorAggregator"/> class.
    /// </summary>
    /// <param name="maxErrorCount">The maximum number of errors to collect. Default is 100.</param>
    public ErrorAggregator(int maxErrorCount = 100)
    {
        if (maxErrorCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxErrorCount), "Max error count must be greater than zero");
        }

        _maxErrorCount = maxErrorCount;
    }

    /// <summary>
    /// Gets the current count of collected errors.
    /// </summary>
    public int ErrorCount => _errors.Count;

    /// <summary>
    /// Gets a value indicating whether the maximum error count has been reached.
    /// </summary>
    public bool HasReachedMaxErrors => _errors.Count >= _maxErrorCount;

    /// <summary>
    /// Gets a value indicating whether any errors have been collected.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Adds a validation error to the aggregator.
    /// </summary>
    /// <param name="error">The validation error to add.</param>
    /// <returns>True if the error was added; false if max error count was reached.</returns>
    public bool AddError(ValidationError error)
    {
        if (error == null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        if (HasReachedMaxErrors)
        {
            return false;
        }

        _errors.Add(error);
        return true;
    }

    /// <summary>
    /// Adds multiple validation errors to the aggregator.
    /// </summary>
    /// <param name="errors">The validation errors to add.</param>
    /// <returns>The number of errors that were added.</returns>
    public int AddErrors(IEnumerable<ValidationError> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var addedCount = 0;
        foreach (var error in errors)
        {
            if (!AddError(error))
            {
                break;
            }
            addedCount++;
        }

        return addedCount;
    }

    /// <summary>
    /// Gets all collected errors.
    /// </summary>
    /// <returns>A read-only list of validation errors.</returns>
    public IReadOnlyList<ValidationError> GetErrors()
    {
        return _errors.AsReadOnly();
    }

    /// <summary>
    /// Gets errors filtered by severity level.
    /// </summary>
    /// <param name="severity">The minimum severity level to include.</param>
    /// <returns>A list of validation errors matching the severity criteria.</returns>
    public IReadOnlyList<ValidationError> GetErrorsBySeverity(ValidationSeverity severity)
    {
        return _errors.Where(e => e.Severity >= severity).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets errors grouped by JSON path.
    /// </summary>
    /// <returns>A dictionary of errors grouped by JSON path.</returns>
    public IReadOnlyDictionary<string, List<ValidationError>> GetErrorsByPath()
    {
        return _errors
            .GroupBy(e => string.IsNullOrEmpty(e.JsonPath) ? "root" : e.JsonPath)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Formats all errors into a single error message.
    /// </summary>
    /// <returns>A formatted error message containing all validation errors.</returns>
    public string FormatErrorMessage()
    {
        if (!HasErrors)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Validation failed with {_errors.Count} error(s):");

        var errorsByPath = GetErrorsByPath();
        foreach (var (path, errors) in errorsByPath.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"  At '{path}':");
            foreach (var error in errors)
            {
                sb.AppendLine($"    [{error.ErrorCode}] {error.Message}");
                if (error.SuggestedFixes.Any())
                {
                    sb.AppendLine($"      Suggestions:");
                    foreach (var fix in error.SuggestedFixes)
                    {
                        sb.AppendLine($"        - {fix}");
                    }
                }
            }
        }

        if (HasReachedMaxErrors)
        {
            sb.AppendLine($"  Note: Maximum error count ({_maxErrorCount}) reached. Additional errors may exist.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats errors into a structured summary.
    /// </summary>
    /// <returns>A summary of validation errors by severity.</returns>
    public string FormatErrorSummary()
    {
        if (!HasErrors)
        {
            return "No validation errors";
        }

        var criticalCount = _errors.Count(e => e.Severity == ValidationSeverity.Critical);
        var errorCount = _errors.Count(e => e.Severity == ValidationSeverity.Error);
        var warningCount = _errors.Count(e => e.Severity == ValidationSeverity.Warning);
        var infoCount = _errors.Count(e => e.Severity == ValidationSeverity.Info);

        var parts = new List<string>();
        if (criticalCount > 0) parts.Add($"{criticalCount} critical");
        if (errorCount > 0) parts.Add($"{errorCount} error(s)");
        if (warningCount > 0) parts.Add($"{warningCount} warning(s)");
        if (infoCount > 0) parts.Add($"{infoCount} info");

        return $"Validation failed: {string.Join(", ", parts)}";
    }

    /// <summary>
    /// Clears all collected errors.
    /// </summary>
    public void Clear()
    {
        _errors.Clear();
    }

    /// <summary>
    /// Creates a ValidationResult from the collected errors.
    /// </summary>
    /// <param name="validatorName">The name of the validator.</param>
    /// <returns>A ValidationResult containing all collected errors.</returns>
    public ValidationResult ToValidationResult(string? validatorName = null)
    {
        return new ValidationResult
        {
            IsValid = !HasErrors,
            Errors = _errors.ToList(),
            ValidatorName = validatorName
        };
    }
}
