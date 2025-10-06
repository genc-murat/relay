namespace Relay.CLI.Migration;

/// <summary>
/// Result of migration operation
/// </summary>
public class MigrationResult
{
    public MigrationStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int FilesModified { get; set; }
    public int LinesChanged { get; set; }
    public int HandlersMigrated { get; set; }
    public bool CreatedBackup { get; set; }
    public string? BackupPath { get; set; }
    public List<MigrationChange> Changes { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public List<string> ManualSteps { get; set; } = new();
}
