namespace Relay.CLI.Migration;

public class BackupMetadata
{
    public string BackupId { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string BackupPath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string ToolVersion { get; set; } = "";
    public int FileCount { get; set; }
    public long TotalSize { get; set; }
}
