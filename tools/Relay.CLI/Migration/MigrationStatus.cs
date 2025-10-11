namespace Relay.CLI.Migration;

/// <summary>
/// Status of the migration operation
/// </summary>
public enum MigrationStatus
{
    /// <summary>
    /// Migration has not started yet
    /// </summary>
    NotStarted,

    /// <summary>
    /// Migration is currently in progress
    /// </summary>
    InProgress,

    /// <summary>
    /// Migration completed successfully
    /// </summary>
    Success,

    /// <summary>
    /// Migration partially completed with some issues
    /// </summary>
    Partial,

    /// <summary>
    /// Migration failed with critical errors
    /// </summary>
    Failed
}
