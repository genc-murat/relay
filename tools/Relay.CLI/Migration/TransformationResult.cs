namespace Relay.CLI.Migration;

/// <summary>
/// Result of file transformation
/// </summary>
public class TransformationResult
{
    /// <summary>
    /// Path to the file that was transformed
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Original file content before transformation
    /// </summary>
    public string OriginalContent { get; set; } = "";

    /// <summary>
    /// New file content after transformation
    /// </summary>
    public string NewContent { get; set; } = "";

    /// <summary>
    /// Indicates whether the file was modified
    /// </summary>
    public bool WasModified { get; set; }

    /// <summary>
    /// Number of lines that were changed
    /// </summary>
    public int LinesChanged { get; set; }

    /// <summary>
    /// Indicates whether the file contains a handler implementation
    /// </summary>
    public bool IsHandler { get; set; }

    /// <summary>
    /// List of specific changes made to the file
    /// </summary>
    public List<MigrationChange> Changes { get; set; } = new();

    /// <summary>
    /// Error message if transformation failed
    /// </summary>
    public string? Error { get; set; }
}
