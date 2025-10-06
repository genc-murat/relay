namespace Relay.CLI.Migration;

/// <summary>
/// Migration issue or warning
/// </summary>
public class MigrationIssue
{
    public IssueSeverity Severity { get; set; }
    public string Message { get; set; } = "";
    public string Code { get; set; } = "";
    public string FilePath { get; set; } = "";
}
