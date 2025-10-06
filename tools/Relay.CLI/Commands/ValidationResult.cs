namespace Relay.CLI.Commands;

public class ValidationResult
{
    public string Type { get; set; } = "";
    public ValidationStatus Status { get; set; }
    public string Message { get; set; } = "";
    public ValidationSeverity Severity { get; set; }
    public string? Suggestion { get; set; }
}
