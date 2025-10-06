namespace Relay.CLI.Plugins;

/// <summary>
/// Core interface that all Relay CLI plugins must implement
/// </summary>
public interface IRelayPlugin
{
    /// <summary>
    /// Plugin name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version (SemVer)
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Short description of what the plugin does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Plugin authors
    /// </summary>
    string[] Authors { get; }

    /// <summary>
    /// Plugin tags for discovery
    /// </summary>
    string[] Tags { get; }

    /// <summary>
    /// Minimum Relay CLI version required
    /// </summary>
    string MinimumRelayVersion { get; }

    /// <summary>
    /// Initialize the plugin with context
    /// </summary>
    Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute the plugin with given arguments
    /// </summary>
    Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleanup resources before unload
    /// </summary>
    Task CleanupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get help text for the plugin
    /// </summary>
    string GetHelp();
}
