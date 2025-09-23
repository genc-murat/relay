using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Result of configuration validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the configuration is valid
    /// </summary>
    public bool IsValid => !Issues.Any(i => i.Severity == ValidationSeverity.Error);

    /// <summary>
    /// List of validation issues found
    /// </summary>
    public List<ValidationIssue> Issues { get; set; } = new();

    /// <summary>
    /// Summary of validation results
    /// </summary>
    public string Summary => IsValid ? "Configuration is valid" : $"Found {ErrorCount} errors and {WarningCount} warnings";

    /// <summary>
    /// Number of error-level issues
    /// </summary>
    public int ErrorCount => Issues.Count(i => i.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Number of warning-level issues
    /// </summary>
    public int WarningCount => Issues.Count(i => i.Severity == ValidationSeverity.Warning);

    /// <summary>
    /// Number of info-level issues
    /// </summary>
    public int InfoCount => Issues.Count(i => i.Severity == ValidationSeverity.Info);

    /// <summary>
    /// Adds a validation issue
    /// </summary>
    public void AddIssue(ValidationSeverity severity, string message, string? category = null)
    {
        Issues.Add(new ValidationIssue
        {
            Severity = severity,
            Message = message,
            Category = category ?? "General"
        });
    }

    /// <summary>
    /// Adds an error-level issue
    /// </summary>
    public void AddError(string message, string? category = null) =>
        AddIssue(ValidationSeverity.Error, message, category);

    /// <summary>
    /// Adds a warning-level issue
    /// </summary>
    public void AddWarning(string message, string? category = null) =>
        AddIssue(ValidationSeverity.Warning, message, category);

    /// <summary>
    /// Adds an info-level issue
    /// </summary>
    public void AddInfo(string message, string? category = null) =>
        AddIssue(ValidationSeverity.Info, message, category);
}

/// <summary>
/// A single validation issue
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Severity level of the issue
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Description of the issue
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Category of the issue
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Optional code or identifier for the issue
    /// </summary>
    public string? Code { get; set; }
}

/// <summary>
/// Severity levels for validation issues
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message
    /// </summary>
    Info,

    /// <summary>
    /// Warning that should be addressed
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevents proper operation
    /// </summary>
    Error
}