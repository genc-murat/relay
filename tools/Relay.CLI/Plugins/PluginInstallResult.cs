namespace Relay.CLI.Plugins;

/// <summary>
/// Result of plugin installation
/// </summary>
public class PluginInstallResult
{
    public bool Success { get; set; }
    public string? PluginName { get; set; }
    public string? Version { get; set; }
    public string? InstalledPath { get; set; }
    public string? Error { get; set; }
}
