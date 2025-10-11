namespace Relay.CLI.Migration;

/// <summary>
/// Severity levels for migration issues
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Informational message
    /// </summary>
    Info,

    /// <summary>
    /// Warning that may require attention
    /// </summary>
    Warning,

    /// <summary>
    /// Error that blocks migration
    /// </summary>
    Error,

    /// <summary>
    /// Critical error requiring immediate attention
    /// </summary>
    Critical
}
