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
