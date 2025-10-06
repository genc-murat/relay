namespace Relay.CLI.Commands;

internal class ValidationIssue
{
    public string Type { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
}
