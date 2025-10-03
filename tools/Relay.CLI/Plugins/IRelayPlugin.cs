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

/// <summary>
/// Logger interface for plugins
/// </summary>
public interface IPluginLogger
{
    void LogTrace(string message);
    void LogDebug(string message);
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
    void LogCritical(string message, Exception? exception = null);
}

/// <summary>
/// File system operations for plugins
/// </summary>
public interface IFileSystem
{
    Task<bool> FileExistsAsync(string path);
    Task<bool> DirectoryExistsAsync(string path);
    Task<string> ReadFileAsync(string path);
    Task WriteFileAsync(string path, string content);
    Task<string[]> GetFilesAsync(string path, string pattern, bool recursive = false);
    Task<string[]> GetDirectoriesAsync(string path);
    Task CreateDirectoryAsync(string path);
    Task DeleteFileAsync(string path);
    Task DeleteDirectoryAsync(string path, bool recursive = false);
    Task CopyFileAsync(string source, string destination);
    Task MoveFileAsync(string source, string destination);
}

/// <summary>
/// Configuration interface for plugins
/// </summary>
public interface IConfiguration
{
    string? this[string key] { get; set; }
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task<bool> ContainsKeyAsync(string key);
    Task RemoveAsync(string key);
}

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
