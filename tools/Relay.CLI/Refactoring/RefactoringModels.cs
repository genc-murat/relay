namespace Relay.CLI.Refactoring;

public class RefactoringOptions
{
    public string ProjectPath { get; set; } = string.Empty;
    public bool DryRun { get; set; }
    public bool Interactive { get; set; }
    public RefactoringSeverity MinimumSeverity { get; set; } = RefactoringSeverity.Info;
    public List<string> SpecificRules { get; set; } = new();
    public List<RefactoringCategory> Categories { get; set; } = new();
    public bool CreateBackup { get; set; } = true;
}

public class RefactoringResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int FilesAnalyzed { get; set; }
    public int SuggestionsCount { get; set; }
    public List<FileRefactoringResult> FileResults { get; set; } = new();
}

public class FileRefactoringResult
{
    public string FilePath { get; set; } = string.Empty;
    public List<RefactoringSuggestion> Suggestions { get; set; } = new();
}

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

public enum RefactoringCategory
{
    Performance,
    Readability,
    Modernization,
    BestPractices,
    Maintainability,
    Security,
    AsyncAwait
}

public enum RefactoringSeverity
{
    Info,
    Suggestion,
    Warning,
    Error
}

public enum RefactoringStatus
{
    Success,
    Partial,
    Failed
}
