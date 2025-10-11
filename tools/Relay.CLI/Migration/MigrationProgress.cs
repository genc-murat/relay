namespace Relay.CLI.Migration;

/// <summary>
/// Progress information for migration operations
/// </summary>
public class MigrationProgress
{
    /// <summary>
    /// Current stage of migration
    /// </summary>
    public MigrationStage Stage { get; set; }

    /// <summary>
    /// Current file being processed
    /// </summary>
    public string? CurrentFile { get; set; }

    /// <summary>
    /// Total number of files to process
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Number of files processed so far
    /// </summary>
    public int ProcessedFiles { get; set; }

    /// <summary>
    /// Percentage complete (0-100)
    /// </summary>
    public double PercentComplete => TotalFiles > 0 ? (ProcessedFiles * 100.0 / TotalFiles) : 0;

    /// <summary>
    /// Current operation message
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Estimated time remaining (if available)
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Elapsed time since start
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Number of files modified
    /// </summary>
    public int FilesModified { get; set; }

    /// <summary>
    /// Number of handlers migrated
    /// </summary>
    public int HandlersMigrated { get; set; }

    /// <summary>
    /// Whether the current operation is running in parallel
    /// </summary>
    public bool IsParallel { get; set; }
}

/// <summary>
/// Stages of the migration process
/// </summary>
public enum MigrationStage
{
    /// <summary>
    /// Initializing migration
    /// </summary>
    Initializing,

    /// <summary>
    /// Analyzing project
    /// </summary>
    Analyzing,

    /// <summary>
    /// Creating backup
    /// </summary>
    CreatingBackup,

    /// <summary>
    /// Transforming package references
    /// </summary>
    TransformingPackages,

    /// <summary>
    /// Transforming code files
    /// </summary>
    TransformingCode,

    /// <summary>
    /// Finalizing migration
    /// </summary>
    Finalizing,

    /// <summary>
    /// Migration completed
    /// </summary>
    Completed
}
