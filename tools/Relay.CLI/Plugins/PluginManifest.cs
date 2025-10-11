namespace Relay.CLI.Plugins;

/// <summary>
/// Plugin manifest (plugin.json)
/// </summary>
public class PluginManifest
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] Authors { get; set; } = Array.Empty<string>();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string MinimumRelayVersion { get; set; } = "2.1.0";
    public Dictionary<string, string> Dependencies { get; set; } = new();
    public string? Repository { get; set; }
    public string? License { get; set; }
    public PluginPermissions? Permissions { get; set; }
}
