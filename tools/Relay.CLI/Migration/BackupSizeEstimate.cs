namespace Relay.CLI.Migration;

/// <summary>
/// Estimated backup size information
/// </summary>
public class BackupSizeEstimate
{
    /// <summary>
    /// Source path being analyzed
    /// </summary>
    public string SourcePath { get; set; } = "";

    /// <summary>
    /// Number of files to backup
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Total size of files in bytes
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Estimated compressed size in bytes
    /// </summary>
    public long EstimatedCompressedSize { get; set; }

    /// <summary>
    /// Error message if estimation failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets total size in human-readable format
    /// </summary>
    public string TotalSizeFormatted => FormatBytes(TotalSize);

    /// <summary>
    /// Gets estimated compressed size in human-readable format
    /// </summary>
    public string CompressedSizeFormatted => FormatBytes(EstimatedCompressedSize);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
