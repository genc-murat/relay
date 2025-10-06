using Microsoft.CodeAnalysis;

namespace Relay.CLI.Commands.Models.Diagnostic;

public class DiagnosticResults
{
    public List<DiagnosticCheck> Checks { get; } = new();
    public int SuccessCount => Checks.SelectMany(c => c.Issues).Count(i => i.Severity == DiagnosticSeverity.Success);
    public int InfoCount => Checks.SelectMany(c => c.Issues).Count(i => i.Severity == DiagnosticSeverity.Info);
    public int WarningCount => Checks.SelectMany(c => c.Issues).Count(i => i.Severity == DiagnosticSeverity.Warning);
    public int ErrorCount => Checks.SelectMany(c => c.Issues).Count(i => i.Severity == DiagnosticSeverity.Error);

    public void AddCheck(DiagnosticCheck check) => Checks.Add(check);
    public bool HasFixableIssues() => Checks.SelectMany(c => c.Issues).Any(i => i.IsFixable);
    public int GetExitCode() => ErrorCount > 0 ? 2 : (WarningCount > 0 ? 1 : 0);
}
