namespace Relay.CLI.Refactoring;

public class FileRefactoringResult
{
    public string FilePath { get; set; } = string.Empty;
    public List<RefactoringSuggestion> Suggestions { get; set; } = new();
}
