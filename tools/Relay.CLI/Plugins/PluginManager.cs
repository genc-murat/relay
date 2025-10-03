using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Relay.CLI.Plugins;

/// <summary>
/// Manages plugin discovery, loading, and lifecycle
/// </summary>
public class PluginManager
{
    private readonly Dictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly string _pluginsDirectory;
    private readonly string _globalPluginsDirectory;

    public PluginManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _globalPluginsDirectory = Path.Combine(appData, "RelayCLI", "plugins");
        _pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".relay", "plugins");

        EnsureDirectoriesExist();
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_globalPluginsDirectory);
        Directory.CreateDirectory(_pluginsDirectory);
    }

    public async Task<List<PluginInfo>> GetInstalledPluginsAsync(bool includeDisabled = false)
    {
        var plugins = new List<PluginInfo>();

        // Scan local plugins
        await ScanPluginsDirectory(_pluginsDirectory, plugins, false);

        // Scan global plugins
        await ScanPluginsDirectory(_globalPluginsDirectory, plugins, true);

        if (!includeDisabled)
        {
            plugins = plugins.Where(p => p.Enabled).ToList();
        }

        return plugins;
    }

    private async Task ScanPluginsDirectory(string directory, List<PluginInfo> plugins, bool isGlobal)
    {
        if (!Directory.Exists(directory))
            return;

        var pluginDirs = Directory.GetDirectories(directory);

        foreach (var pluginDir in pluginDirs)
        {
            var manifestPath = Path.Combine(pluginDir, "plugin.json");
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);

                if (manifest != null)
                {
                    plugins.Add(new PluginInfo
                    {
                        Name = manifest.Name,
                        Version = manifest.Version,
                        Description = manifest.Description,
                        Authors = manifest.Authors,
                        Tags = manifest.Tags,
                        Path = pluginDir,
                        IsGlobal = isGlobal,
                        Enabled = true,
                        Manifest = manifest
                    });
                }
            }
            catch
            {
                // Skip invalid plugins
            }
        }
    }

    public async Task<IRelayPlugin?> LoadPluginAsync(string pluginName, IPluginContext context)
    {
        // Check if already loaded
        if (_loadedPlugins.ContainsKey(pluginName))
        {
            return _loadedPlugins[pluginName].Instance;
        }

        // Find plugin
        var pluginInfo = (await GetInstalledPluginsAsync(true))
            .FirstOrDefault(p => p.Name == pluginName);

        if (pluginInfo == null)
            return null;

        try
        {
            // Load assembly
            var assemblyPath = FindPluginAssembly(pluginInfo.Path);
            if (assemblyPath == null)
                return null;

            var loadContext = new PluginLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            // Find plugin type
            var pluginType = FindPluginType(assembly);
            if (pluginType == null)
                return null;

            // Create instance
            var instance = Activator.CreateInstance(pluginType) as IRelayPlugin;
            if (instance == null)
                return null;

            // Initialize
            var initialized = await instance.InitializeAsync(context);
            if (!initialized)
                return null;

            // Store loaded plugin
            _loadedPlugins[pluginName] = new LoadedPlugin
            {
                Name = pluginName,
                Instance = instance,
                LoadContext = loadContext,
                Assembly = assembly
            };

            return instance;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UnloadPluginAsync(string pluginName)
    {
        if (!_loadedPlugins.ContainsKey(pluginName))
            return false;

        var plugin = _loadedPlugins[pluginName];

        try
        {
            await plugin.Instance.CleanupAsync();
            plugin.LoadContext.Unload();
            _loadedPlugins.Remove(pluginName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string? FindPluginAssembly(string pluginPath)
    {
        var dllFiles = Directory.GetFiles(pluginPath, "*.dll", SearchOption.AllDirectories);
        
        // Look for the main plugin DLL (excluding dependencies)
        foreach (var dll in dllFiles)
        {
            if (dll.Contains("Relay.CLI.Sdk"))
                continue;

            var fileName = Path.GetFileName(dll);
            if (fileName.StartsWith("relay-plugin-") || fileName.Contains("Plugin"))
            {
                return dll;
            }
        }

        // Fallback to first DLL
        return dllFiles.FirstOrDefault();
    }

    private Type? FindPluginType(Assembly assembly)
    {
        try
        {
            var types = assembly.GetTypes();
            
            // Look for type with RelayPlugin attribute
            var pluginType = types.FirstOrDefault(t => 
                t.GetCustomAttribute<RelayPluginAttribute>() != null);

            if (pluginType != null)
                return pluginType;

            // Fallback to type implementing IRelayPlugin
            return types.FirstOrDefault(t => 
                typeof(IRelayPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PluginInstallResult> InstallPluginAsync(string source, string? version, bool global)
    {
        var result = new PluginInstallResult();

        try
        {
            // Determine source type (local path, NuGet package, URL)
            if (Directory.Exists(source))
            {
                // Local directory
                result = await InstallFromLocalAsync(source, global);
            }
            else if (File.Exists(source) && source.EndsWith(".zip"))
            {
                // ZIP file
                result = await InstallFromZipAsync(source, global);
            }
            else
            {
                // Assume NuGet package name
                result = await InstallFromNuGetAsync(source, version, global);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    private async Task<PluginInstallResult> InstallFromLocalAsync(string sourcePath, bool global)
    {
        var result = new PluginInstallResult();

        // Read manifest
        var manifestPath = Path.Combine(sourcePath, "plugin.json");
        if (!File.Exists(manifestPath))
        {
            result.Success = false;
            result.Error = "plugin.json not found";
            return result;
        }

        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);

        if (manifest == null)
        {
            result.Success = false;
            result.Error = "Invalid plugin.json";
            return result;
        }

        // Copy to plugins directory
        var targetDir = global ? _globalPluginsDirectory : _pluginsDirectory;
        var pluginDir = Path.Combine(targetDir, manifest.Name);

        if (Directory.Exists(pluginDir))
        {
            Directory.Delete(pluginDir, true);
        }

        CopyDirectory(sourcePath, pluginDir);

        result.Success = true;
        result.PluginName = manifest.Name;
        result.Version = manifest.Version;
        result.InstalledPath = pluginDir;

        return result;
    }

    private async Task<PluginInstallResult> InstallFromZipAsync(string zipPath, bool global)
    {
        var result = new PluginInstallResult();

        // Extract to temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempDir);

        // Install from temp
        result = await InstallFromLocalAsync(tempDir, global);

        // Cleanup
        Directory.Delete(tempDir, true);

        return result;
    }

    private async Task<PluginInstallResult> InstallFromNuGetAsync(string packageName, string? version, bool global)
    {
        var result = new PluginInstallResult
        {
            Success = false,
            Error = "NuGet installation not yet implemented"
        };

        // TODO: Implement NuGet package download
        await Task.CompletedTask;

        return result;
    }

    public async Task<bool> UninstallPluginAsync(string pluginName, bool global)
    {
        var plugins = await GetInstalledPluginsAsync(true);
        var plugin = plugins.FirstOrDefault(p => p.Name == pluginName && p.IsGlobal == global);

        if (plugin == null)
            return false;

        try
        {
            // Unload if loaded
            await UnloadPluginAsync(pluginName);

            // Delete directory
            if (Directory.Exists(plugin.Path))
            {
                Directory.Delete(plugin.Path, true);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
}

/// <summary>
/// Custom AssemblyLoadContext for plugin isolation
/// </summary>
internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}

internal class LoadedPlugin
{
    public string Name { get; set; } = "";
    public IRelayPlugin Instance { get; set; } = null!;
    public PluginLoadContext LoadContext { get; set; } = null!;
    public Assembly Assembly { get; set; } = null!;
}
