namespace Relay.CLI.Migration;

/// <summary>
/// Type of change made during migration
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// New code or configuration added
    /// </summary>
    Add,

    /// <summary>
    /// Existing code or configuration removed
    /// </summary>
    Remove,

    /// <summary>
    /// Existing code or configuration modified
    /// </summary>
    Modify
}
