namespace Relay.CLI.Plugins;

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
