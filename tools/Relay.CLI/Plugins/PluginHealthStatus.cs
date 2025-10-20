namespace Relay.CLI.Plugins;

/// <summary>
/// Plugin health status
/// </summary>
public enum PluginHealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy,
    Disabled
}