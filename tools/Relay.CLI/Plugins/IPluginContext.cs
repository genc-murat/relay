namespace Relay.CLI.Plugins;

/// <summary>
/// Context provided to plugins for accessing CLI services
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Logger for plugin diagnostics
    /// </summary>
    IPluginLogger Logger { get; }

    /// <summary>
    /// File system operations
    /// </summary>
    IFileSystem FileSystem { get; }

    /// <summary>
    /// Configuration access
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Service provider for dependency injection
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// CLI version information
    /// </summary>
    string CliVersion { get; }

    /// <summary>
    /// Current working directory
    /// </summary>
    string WorkingDirectory { get; }

    /// <summary>
    /// Get a service from the DI container
    /// </summary>
    Task<T?> GetServiceAsync<T>() where T : class;

    /// <summary>
    /// Get a required service from the DI container
    /// </summary>
    Task<T> GetRequiredServiceAsync<T>() where T : class;

    /// <summary>
    /// Get a configuration value
    /// </summary>
    Task<string?> GetSettingAsync(string key);

    /// <summary>
    /// Set a configuration value
    /// </summary>
    Task SetSettingAsync(string key, string value);
}
