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

/// <summary>
/// Analysis result from project scanning
/// </summary>
public class AnalysisResult
{
    public string ProjectPath { get; set; } = "";
    public DateTime AnalysisDate { get; set; }
    public bool CanMigrate { get; set; } = true;
    public int FilesAffected { get; set; }
    public int HandlersFound { get; set; }
    public int RequestsFound { get; set; }
    public int NotificationsFound { get; set; }
    public int PipelineBehaviorsFound { get; set; }
    public bool HasCustomMediator { get; set; }
    public bool HasCustomBehaviors { get; set; }
    public List<PackageReference> PackageReferences { get; set; } = new();
    public List<MigrationIssue> Issues { get; set; } = new();
    public List<string> FilesWithMediatR { get; set; } = new();
}

/// <summary>
/// Package reference information
/// </summary>
public class PackageReference
{
    public string Name { get; set; } = "";
    public string CurrentVersion { get; set; } = "";
    public string ProjectFile { get; set; } = "";
}

/// <summary>
/// Migration issue or warning
/// </summary>
public class MigrationIssue
{
    public IssueSeverity Severity { get; set; }
    public string Message { get; set; } = "";
    public string Code { get; set; } = "";
    public string FilePath { get; set; } = "";
}

public enum IssueSeverity
{
    Info,
    Warning,
    Error
}

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

public enum MigrationStatus
{
    NotStarted,
    InProgress,
    Success,
    Partial,
    Failed
}

/// <summary>
/// Individual change applied during migration
/// </summary>
public class MigrationChange
{
    public string Category { get; set; } = "";
    public ChangeType Type { get; set; }
    public string Description { get; set; } = "";
    public string FilePath { get; set; } = "";
}

public enum ChangeType
{
    Add,
    Remove,
    Modify
}

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
