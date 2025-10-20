using System.Reflection;

namespace Relay.CLI.Plugins;

internal class LoadedPlugin
{
    public string Name { get; set; } = "";
    public IRelayPlugin Instance { get; set; } = null!;
    public PluginLoadContext LoadContext { get; set; } = null!;
    public Assembly Assembly { get; set; } = null!;
    public DateTime LastLoadTime { get; set; } = DateTime.UtcNow;
}
