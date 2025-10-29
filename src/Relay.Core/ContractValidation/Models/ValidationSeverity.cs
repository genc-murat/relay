namespace Relay.Core.ContractValidation.Models;

/// <summary>
/// Represents the severity level of a validation error.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message, does not indicate a validation failure.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning message, indicates a potential issue but not a validation failure.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error message, indicates a validation failure.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical error message, indicates a severe validation failure.
    /// </summary>
    Critical = 3
}
