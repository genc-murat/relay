namespace Relay.CLI.Migration;

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
