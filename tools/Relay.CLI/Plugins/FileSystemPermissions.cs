namespace Relay.CLI.Plugins;

/// <summary>
/// File system permissions configuration
/// </summary>
public class FileSystemPermissions
{
    public bool Read { get; set; } = false;
    public bool Write { get; set; } = false;
    public bool Delete { get; set; } = false;
    public string[]? AllowedPaths { get; set; } = Array.Empty<string>();
    public string[]? DeniedPaths { get; set; } = Array.Empty<string>();
}
