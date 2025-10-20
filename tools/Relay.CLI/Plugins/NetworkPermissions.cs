namespace Relay.CLI.Plugins;

/// <summary>
/// Network permissions configuration
/// </summary>
public class NetworkPermissions
{
    public bool Http { get; set; } = false;
    public bool Https { get; set; } = false;
    public string[]? AllowedHosts { get; set; } = Array.Empty<string>();
    public string[]? DeniedHosts { get; set; } = Array.Empty<string>();
}