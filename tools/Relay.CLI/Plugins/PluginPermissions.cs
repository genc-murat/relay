namespace Relay.CLI.Plugins;

/// <summary>
/// Plugin permissions configuration
/// </summary>
public class PluginPermissions
{
    public FileSystemPermissions? FileSystem { get; set; }
    public NetworkPermissions? Network { get; set; }
    public long MaxMemoryBytes { get; set; } = 100 * 1024 * 1024; // Default 100MB
    public long MaxExecutionTimeMs { get; set; } = 300000; // Default 5 minutes (300,000 ms)
    // Add other permission types as needed
}
