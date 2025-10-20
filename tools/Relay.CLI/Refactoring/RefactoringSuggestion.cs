namespace Relay.CLI.Refactoring;

public class RefactoringSuggestion
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RefactoringCategory Category { get; set; }
    public RefactoringSeverity Severity { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public string OriginalCode { get; set; } = string.Empty;
    public string SuggestedCode { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public object? Context { get; set; }
}
