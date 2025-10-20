namespace Relay.CLI.Plugins;

/// <summary>
/// Result of security validation
/// </summary>
public class SecurityValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new List<string>();
}
