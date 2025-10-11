namespace Relay.CLI.Migration;

/// <summary>
/// Result of migration operation
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Overall status of the migration
    /// </summary>
    public MigrationStatus Status { get; set; }

    /// <summary>
    /// When the migration started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the migration completed
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Total duration of the migration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Number of files that were modified
    /// </summary>
    public int FilesModified { get; set; }

    /// <summary>
    /// Total number of lines changed across all files
    /// </summary>
    public int LinesChanged { get; set; }

    /// <summary>
    /// Number of MediatR handlers that were migrated
    /// </summary>
    public int HandlersMigrated { get; set; }

    /// <summary>
    /// Whether a backup was created before migration
    /// </summary>
    public bool CreatedBackup { get; set; }

    /// <summary>
    /// Path to the backup directory if created
    /// </summary>
    public string? BackupPath { get; set; }

    /// <summary>
    /// Detailed list of all changes made during migration
    /// </summary>
    public List<MigrationChange> Changes { get; set; } = new();

    /// <summary>
    /// List of issues encountered during migration
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// List of manual steps required after migration
    /// </summary>
    public List<string> ManualSteps { get; set; } = new();
}
