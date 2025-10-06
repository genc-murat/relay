namespace Relay.Core.Diagnostics.Validation;

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