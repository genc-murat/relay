namespace Relay.CLI.Plugins;

/// <summary>
/// Attribute to mark a class as a Relay plugin
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RelayPluginAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }

    public RelayPluginAttribute(string name, string version)
    {
        Name = name;
        Version = version;
    }
}
