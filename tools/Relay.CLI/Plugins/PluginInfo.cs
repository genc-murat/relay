namespace Relay.CLI.Plugins;

/// <summary>
/// Plugin information
/// </summary>
public class PluginInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] Authors { get; set; } = Array.Empty<string>();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Path { get; set; } = "";
    public bool IsGlobal { get; set; }
    public bool Enabled { get; set; }
    public PluginManifest? Manifest { get; set; }
}
