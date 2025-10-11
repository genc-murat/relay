namespace Relay.CLI.Migration;

/// <summary>
/// Metadata information for a migration backup
/// </summary>
public class BackupMetadata
{
    /// <summary>
    /// Unique identifier for this backup
    /// </summary>
    public string BackupId { get; set; } = "";

    /// <summary>
    /// Original source path that was backed up
    /// </summary>
    public string SourcePath { get; set; } = "";

    /// <summary>
    /// Path where backup files are stored
    /// </summary>
    public string BackupPath { get; set; } = "";

    /// <summary>
    /// Timestamp when backup was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Version of the CLI tool that created this backup
    /// </summary>
    public string ToolVersion { get; set; } = "";

    /// <summary>
    /// Number of files included in the backup
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Total size of backed up files in bytes
    /// </summary>
    public long TotalSize { get; set; }
}
