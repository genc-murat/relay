namespace Relay.CLI.Refactoring;

public class ApplyResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int FilesModified { get; set; }
    public int RefactoringsApplied { get; set; }
    public RefactoringStatus Status { get; set; }
    public string? Error { get; set; }
}
