namespace Relay.CLI.Migration;

/// <summary>
/// Migration options and configuration
/// </summary>
public class MigrationOptions
{
    /// <summary>
    /// Source framework to migrate from (default: MediatR)
    /// </summary>
    public string SourceFramework { get; set; } = "MediatR";

    /// <summary>
    /// Target framework to migrate to (default: Relay)
    /// </summary>
    public string TargetFramework { get; set; } = "Relay";

    /// <summary>
    /// Path to the project to migrate
    /// </summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>
    /// Only analyze the project without making changes
    /// </summary>
    public bool AnalyzeOnly { get; set; }

    /// <summary>
    /// Show what changes would be made without applying them
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Show detailed preview of changes
    /// </summary>
    public bool ShowPreview { get; set; }

    /// <summary>
    /// Create a backup before applying changes (default: true)
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// Directory path for backup files (default: .backup)
    /// </summary>
    public string BackupPath { get; set; } = ".backup";

    /// <summary>
    /// Prompt for confirmation on each change
    /// </summary>
    public bool Interactive { get; set; }

    /// <summary>
    /// Apply aggressive optimizations during migration
    /// </summary>
    public bool Aggressive { get; set; }
}
