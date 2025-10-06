namespace Relay.CLI.Migration;

/// <summary>
/// Migration options and configuration
/// </summary>
public class MigrationOptions
{
    public string SourceFramework { get; set; } = "MediatR";
    public string TargetFramework { get; set; } = "Relay";
    public string ProjectPath { get; set; } = "";
    public bool AnalyzeOnly { get; set; }
    public bool DryRun { get; set; }
    public bool ShowPreview { get; set; }
    public bool CreateBackup { get; set; } = true;
    public string BackupPath { get; set; } = ".backup";
    public bool Interactive { get; set; }
    public bool Aggressive { get; set; }
}
