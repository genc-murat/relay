namespace Relay.Core.Diagnostics.Validation;

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
