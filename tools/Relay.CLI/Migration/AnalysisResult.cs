namespace Relay.CLI.Migration;

/// <summary>
/// Analysis result from project scanning
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// Path to the project that was analyzed
    /// </summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>
    /// When the analysis was performed
    /// </summary>
    public DateTime AnalysisDate { get; set; }

    /// <summary>
    /// Whether the project can be safely migrated
    /// </summary>
    public bool CanMigrate { get; set; } = true;

    /// <summary>
    /// Number of files that will be affected by migration
    /// </summary>
    public int FilesAffected { get; set; }

    /// <summary>
    /// Number of MediatR handlers detected
    /// </summary>
    public int HandlersFound { get; set; }

    /// <summary>
    /// Number of MediatR requests detected
    /// </summary>
    public int RequestsFound { get; set; }

    /// <summary>
    /// Number of MediatR notifications detected
    /// </summary>
    public int NotificationsFound { get; set; }

    /// <summary>
    /// Number of pipeline behaviors detected
    /// </summary>
    public int PipelineBehaviorsFound { get; set; }

    /// <summary>
    /// Whether the project has a custom IMediator implementation
    /// </summary>
    public bool HasCustomMediator { get; set; }

    /// <summary>
    /// Whether the project has custom pipeline behaviors
    /// </summary>
    public bool HasCustomBehaviors { get; set; }

    /// <summary>
    /// MediatR package references found in the project
    /// </summary>
    public List<PackageReference> PackageReferences { get; set; } = new();

    /// <summary>
    /// Issues and warnings discovered during analysis
    /// </summary>
    public List<MigrationIssue> Issues { get; set; } = new();

    /// <summary>
    /// List of files that contain MediatR usage
    /// </summary>
    public List<string> FilesWithMediatR { get; set; } = new();
}
