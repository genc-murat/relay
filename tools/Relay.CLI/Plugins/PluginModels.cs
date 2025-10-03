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
}

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

/// <summary>
/// Plugin context implementation
/// </summary>
public class PluginContext : IPluginContext
{
    public IPluginLogger Logger { get; }
    public IFileSystem FileSystem { get; }
    public IConfiguration Configuration { get; }
    public IServiceProvider Services { get; }
    public string CliVersion { get; }
    public string WorkingDirectory { get; }

    public PluginContext(
        IPluginLogger logger,
        IFileSystem fileSystem,
        IConfiguration configuration,
        IServiceProvider services,
        string cliVersion,
        string workingDirectory)
    {
        Logger = logger;
        FileSystem = fileSystem;
        Configuration = configuration;
        Services = services;
        CliVersion = cliVersion;
        WorkingDirectory = workingDirectory;
    }

    public Task<T?> GetServiceAsync<T>() where T : class
    {
        var service = Services.GetService(typeof(T)) as T;
        return Task.FromResult(service);
    }

    public Task<T> GetRequiredServiceAsync<T>() where T : class
    {
        var service = Services.GetService(typeof(T)) as T;
        if (service == null)
            throw new InvalidOperationException($"Service of type {typeof(T).Name} not found");
        return Task.FromResult(service);
    }

    public Task<string?> GetSettingAsync(string key)
    {
        return Task.FromResult(Configuration[key]);
    }

    public Task SetSettingAsync(string key, string value)
    {
        Configuration[key] = value;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Simple plugin logger implementation
/// </summary>
public class PluginLogger : IPluginLogger
{
    private readonly string _pluginName;

    public PluginLogger(string pluginName)
    {
        _pluginName = pluginName;
    }

    public void LogTrace(string message) => Log("TRACE", message);
    public void LogDebug(string message) => Log("DEBUG", message);
    public void LogInformation(string message) => Log("INFO", message);
    public void LogWarning(string message) => Log("WARN", message);
    public void LogError(string message, Exception? exception = null)
    {
        Log("ERROR", message);
        if (exception != null)
            Console.WriteLine($"  Exception: {exception.Message}");
    }
    public void LogCritical(string message, Exception? exception = null)
    {
        Log("CRITICAL", message);
        if (exception != null)
            Console.WriteLine($"  Exception: {exception.Message}");
    }

    private void Log(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"[{timestamp}] [{level}] [{_pluginName}] {message}");
    }
}

/// <summary>
/// Simple file system implementation
/// </summary>
public class PluginFileSystem : IFileSystem
{
    public Task<bool> FileExistsAsync(string path) => Task.FromResult(File.Exists(path));
    public Task<bool> DirectoryExistsAsync(string path) => Task.FromResult(Directory.Exists(path));
    public Task<string> ReadFileAsync(string path) => File.ReadAllTextAsync(path);
    public Task WriteFileAsync(string path, string content) => File.WriteAllTextAsync(path, content);
    public Task<string[]> GetFilesAsync(string path, string pattern, bool recursive = false)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Task.FromResult(Directory.GetFiles(path, pattern, searchOption));
    }
    public Task<string[]> GetDirectoriesAsync(string path) => Task.FromResult(Directory.GetDirectories(path));
    public Task CreateDirectoryAsync(string path)
    {
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }
    public Task DeleteFileAsync(string path)
    {
        File.Delete(path);
        return Task.CompletedTask;
    }
    public Task DeleteDirectoryAsync(string path, bool recursive = false)
    {
        Directory.Delete(path, recursive);
        return Task.CompletedTask;
    }
    public Task CopyFileAsync(string source, string destination)
    {
        File.Copy(source, destination, true);
        return Task.CompletedTask;
    }
    public Task MoveFileAsync(string source, string destination)
    {
        File.Move(source, destination, true);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Simple configuration implementation
/// </summary>
public class PluginConfiguration : IConfiguration
{
    private readonly Dictionary<string, string> _settings = new();

    public string? this[string key]
    {
        get => _settings.TryGetValue(key, out var value) ? value : null;
        set
        {
            if (value != null)
                _settings[key] = value;
            else
                _settings.Remove(key);
        }
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (!_settings.TryGetValue(key, out var value))
            return Task.FromResult<T?>(default);

        try
        {
            var converted = (T?)Convert.ChangeType(value, typeof(T));
            return Task.FromResult(converted);
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    public Task SetAsync<T>(string key, T value)
    {
        if (value != null)
            _settings[key] = value.ToString() ?? "";
        return Task.CompletedTask;
    }

    public Task<bool> ContainsKeyAsync(string key) => Task.FromResult(_settings.ContainsKey(key));

    public Task RemoveAsync(string key)
    {
        _settings.Remove(key);
        return Task.CompletedTask;
    }
}
