namespace Relay.CLI.Migration;

/// <summary>
/// Result of file transformation
/// </summary>
public class TransformationResult
{
    public string FilePath { get; set; } = "";
    public string OriginalContent { get; set; } = "";
    public string NewContent { get; set; } = "";
    public bool WasModified { get; set; }
    public int LinesChanged { get; set; }
    public bool IsHandler { get; set; }
    public List<MigrationChange> Changes { get; set; } = new();
    public string? Error { get; set; }
}
