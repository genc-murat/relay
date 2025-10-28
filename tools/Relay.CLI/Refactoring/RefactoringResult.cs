namespace Relay.CLI.Refactoring;

public class RefactoringResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int FilesAnalyzed { get; set; }
    public int FilesSkipped { get; set; }
    public int SuggestionsCount { get; set; }
    public List<FileRefactoringResult> FileResults { get; set; } = new();
}
