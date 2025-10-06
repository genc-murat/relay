namespace Relay.CLI.Commands;

public class DiagnosticIssue
{
    public string Message { get; set; } = "";
    public DiagnosticSeverity Severity { get; set; }
    public string Code { get; set; } = "";
    public bool IsFixable { get; set; }
}
