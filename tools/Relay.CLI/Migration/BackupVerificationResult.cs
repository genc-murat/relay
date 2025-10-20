namespace Relay.CLI.Migration;

/// <summary>
/// Result of backup verification
/// </summary>
public class BackupVerificationResult
{
    /// <summary>
    /// Path to the verified backup
    /// </summary>
    public string BackupPath { get; set; } = "";

    /// <summary>
    /// Whether the backup is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Backup metadata
    /// </summary>
    public BackupMetadata? Metadata { get; set; }

    /// <summary>
    /// Number of files found in backup
    /// </summary>
    public int FilesFound { get; set; }

    /// <summary>
    /// Total size of files in backup
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Whether a compressed archive exists
    /// </summary>
    public bool HasCompressedArchive { get; set; }

    /// <summary>
    /// Size of compressed archive
    /// </summary>
    public long CompressedSize { get; set; }

    /// <summary>
    /// List of errors found during verification
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of warnings found during verification
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
