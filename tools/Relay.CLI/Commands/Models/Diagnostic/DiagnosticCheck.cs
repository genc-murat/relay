namespace Relay.CLI.Commands.Models.Diagnostic;

public class DiagnosticCheck
{
    public string Category { get; set; } = "";
    public List<DiagnosticIssue> Issues { get; } = new();

    public void AddIssue(string message, DiagnosticSeverity severity, string code, bool isFixable = false)
    {
        Issues.Add(new DiagnosticIssue
        {
            Message = message,
            Severity = severity,
            Code = code,
            IsFixable = isFixable
        });
    }

    public void AddSuccess(string message) => AddIssue(message, DiagnosticSeverity.Success, "SUCCESS");
    public void AddInfo(string message) => AddIssue(message, DiagnosticSeverity.Info, "INFO");
}
