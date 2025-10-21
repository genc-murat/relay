using System.Reflection;
using System.Text.Json;

namespace Relay.CLI.Plugins;

/// <summary>
/// Manages plugin discovery, loading, and lifecycle
/// </summary>
public class PluginManager : IDisposable
{
    private readonly Dictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly string _pluginsDirectory;
    private readonly string _globalPluginsDirectory;
    private readonly PluginSecurityValidator _securityValidator;
    private readonly PluginHealthMonitor _healthMonitor;
    private readonly LazyPluginLoader _lazyPluginLoader;
    private readonly IPluginLogger _logger;
    private bool _disposed = false;

    public PluginManager(IPluginLogger logger)
    {
        _logger = logger;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _globalPluginsDirectory = Path.Combine(appData, "RelayCLI", "plugins");
        _pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".relay", "plugins");

        // Initialize security and health monitoring components
        _securityValidator = new PluginSecurityValidator(logger);
        _healthMonitor = new PluginHealthMonitor(logger);
        _lazyPluginLoader = new LazyPluginLoader(this, logger);

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
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginManager));

        // Check if already loaded
        if (_loadedPlugins.ContainsKey(pluginName))
        {
            var existingPlugin = _loadedPlugins[pluginName];

            // Check if the plugin is healthy before returning
            if (_healthMonitor.IsHealthy(pluginName))
            {
                // Re-initialize to ensure plugin is properly initialized with the new context
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // 3 second timeout for re-initialization
                    var initialized = await existingPlugin.Instance.InitializeAsync(context, cts.Token);

                    if (!initialized)
                    {
                        _logger.LogError($"Plugin {pluginName} failed to re-initialize");
                        await UnloadPluginAsync(pluginName);
                        return null;
                    }

                    return existingPlugin.Instance;
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogError($"Plugin {pluginName} re-initialization timed out", ex);
                    _healthMonitor.RecordFailure(pluginName, ex);
                    await UnloadPluginAsync(pluginName);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error re-initializing plugin {pluginName}: {ex.Message}", ex);
                    _healthMonitor.RecordFailure(pluginName, ex);
                    await UnloadPluginAsync(pluginName);
                    return null;
                }
            }
            else
            {
                _logger.LogWarning($"Plugin {pluginName} is not healthy, attempting reload");

                // Try to restart the plugin automatically if it's disabled
                if (_healthMonitor.AttemptRestart(pluginName))
                {
                    _logger.LogInformation($"Successfully initiated restart for plugin: {pluginName}");
                    await UnloadPluginAsync(pluginName);
                }
                else
                {
                    _logger.LogWarning($"Could not initiate restart for plugin: {pluginName}");
                    await UnloadPluginAsync(pluginName);
                    return null;
                }
            }
        }

        // Find plugin
        var pluginInfo = (await GetInstalledPluginsAsync(true))
            .FirstOrDefault(p => p.Name == pluginName);

        if (pluginInfo == null)
        {
            _logger.LogError($"Plugin {pluginName} not found");
            return null;
        }

        // Check health before attempting to load
        if (!_healthMonitor.IsHealthy(pluginName))
        {
            _logger.LogError($"Plugin {pluginName} is not healthy and cannot be loaded");
            return null;
        }

        try
        {
            // Load assembly
            var assemblyPath = FindPluginAssembly(pluginInfo.Path);
            if (assemblyPath == null)
            {
                _logger.LogError($"Could not find plugin assembly for {pluginName}");
                return null;
            }

            // Validate plugin security before loading
            var validationResult = await _securityValidator.ValidatePluginAsync(assemblyPath, pluginInfo);
            if (!validationResult.IsValid)
            {
                _logger.LogError($"Plugin {pluginName} failed security validation");
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogError(error);
                }
                return null;
            }

            var loadContext = new PluginLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            // Find plugin type
            var pluginType = FindPluginType(assembly);
            if (pluginType == null)
            {
                _logger.LogError($"Could not find plugin type in assembly for {pluginName}");
                return null;
            }

            // Create instance
            var instance = Activator.CreateInstance(pluginType) as IRelayPlugin;
            if (instance == null)
            {
                _logger.LogError($"Could not create plugin instance for {pluginName}");
                return null;
            }

            // Initialize with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // 2 minute timeout for initialization
            var initialized = await instance.InitializeAsync(context, cts.Token);
            
            if (!initialized)
            {
                _logger.LogError($"Plugin {pluginName} failed to initialize");
                return null;
            }

            // Store loaded plugin
            _loadedPlugins[pluginName] = new LoadedPlugin
            {
                Name = pluginName,
                Instance = instance,
                LoadContext = loadContext,
                Assembly = assembly,
                LastLoadTime = DateTime.UtcNow
            };

            // Record successful load in health monitor
            _healthMonitor.RecordSuccess(pluginName);
            _healthMonitor.ResetRestartCount(pluginName); // Reset restart count after successful load

            _logger.LogInformation($"Successfully loaded plugin: {pluginName}");
            return instance;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading plugin {pluginName}: {ex.Message}", ex);
            _healthMonitor.RecordFailure(pluginName, ex);
            return null;
        }
    }

    public async Task<bool> UnloadPluginAsync(string pluginName)
    {
        if (_disposed || !_loadedPlugins.ContainsKey(pluginName))
            return false;

        var plugin = _loadedPlugins[pluginName];

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5 second timeout for cleanup
            await plugin.Instance.CleanupAsync(cts.Token);

            // Check if cleanup was cancelled
            if (cts.Token.IsCancellationRequested)
            {
                _logger.LogWarning($"Plugin {pluginName} cleanup was cancelled");
                _healthMonitor.RecordFailure(pluginName, new OperationCanceledException("Cleanup was cancelled"));
                // Still remove from loaded plugins even if cleanup was cancelled
                _loadedPlugins.Remove(pluginName);
                return false;
            }

            plugin.LoadContext.Unload();
            _loadedPlugins.Remove(pluginName);

            _logger.LogInformation($"Successfully unloaded plugin: {pluginName}");
            return true;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Plugin {pluginName} cleanup timed out", ex);
            _healthMonitor.RecordFailure(pluginName, ex);
            // Still remove from loaded plugins even if cleanup timed out
            _loadedPlugins.Remove(pluginName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during plugin {pluginName} cleanup: {ex.Message}", ex);
            _healthMonitor.RecordFailure(pluginName, ex);
            // Still remove from loaded plugins even if cleanup failed
            _loadedPlugins.Remove(pluginName);
            return false;
        }
    }

    private string? FindPluginAssembly(string pluginPath)
    {
        var dllFiles = Directory.GetFiles(pluginPath, "*.dll", SearchOption.AllDirectories);
        
        // Look for the main plugin DLL (excluding dependencies)
        // Priority order: relay-plugin-* prefix > *Plugin* filename > first available DLL
        
        // First, look for files with "relay-plugin-" prefix
        foreach (var dll in dllFiles)
        {
            if (dll.Contains("Relay.CLI.Sdk"))
                continue;

            var fileName = Path.GetFileName(dll);
            if (fileName.StartsWith("relay-plugin-"))
            {
                return dll;
            }
        }
        
        // Second, look for files containing "Plugin" in the name
        foreach (var dll in dllFiles)
        {
            if (dll.Contains("Relay.CLI.Sdk"))
                continue;

            var fileName = Path.GetFileName(dll);
            if (fileName.Contains("Plugin"))
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

    /// <summary>
    /// Executes a plugin with safety measures including timeout and error handling
    /// </summary>
    /// <param name="pluginName">Name of the plugin to execute</param>
    /// <param name="args">Arguments to pass to the plugin</param>
    /// <param name="context">Plugin context</param>
    /// <param name="timeout">Execution timeout (default 5 minutes)</param>
    /// <returns>Exit code from the plugin</returns>
    public async Task<int> ExecutePluginAsync(string pluginName, string[] args, IPluginContext context, TimeSpan? timeout = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginManager));

        timeout ??= TimeSpan.FromMinutes(5); // Default 5-minute timeout

        // Check if the plugin is healthy before executing
        if (!_healthMonitor.IsHealthy(pluginName))
        {
            _logger.LogError($"Cannot execute plugin {pluginName} - it is not in a healthy state");
            return -1; // Error code for unhealthy plugin
        }

        // Load the plugin if not already loaded
        var plugin = await LoadPluginAsync(pluginName, context);
        if (plugin == null)
        {
            _logger.LogError($"Could not load plugin {pluginName} for execution");
            _healthMonitor.RecordFailure(pluginName, new InvalidOperationException($"Could not load plugin {pluginName}"));
            return -1;
        }

        try
        {
            // Create a plugin sandbox for execution
            var permissions = _securityValidator.GetPluginPermissions(pluginName);
            using var sandbox = new PluginSandbox(_logger, permissions);

            // Execute with timeout and resource limits
            using var cts = new CancellationTokenSource(timeout.Value);
            int? result = await sandbox.ExecuteWithResourceLimitsAsync<int>(async () =>
            {
                return await sandbox.ExecuteInSandboxAsync<int>(async () =>
                {
                    return await plugin.ExecuteAsync(args, cts.Token);
                }, cts.Token);
            }, cts.Token);

            // Record success if execution completed
            _healthMonitor.RecordSuccess(pluginName);
            _healthMonitor.ResetRestartCount(pluginName); // Reset restart count after successful execution
            return result ?? -1;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError($"Plugin {pluginName} execution timed out: {ex.Message}", ex);
            _healthMonitor.RecordFailure(pluginName, ex);
            return -1;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error executing plugin {pluginName}: {ex.Message}", ex);
            _healthMonitor.RecordFailure(pluginName, ex);
            return -1;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Unload all loaded plugins
            var pluginNames = _loadedPlugins.Keys.ToList();
            foreach (var pluginName in pluginNames)
            {
                _ = UnloadPluginAsync(pluginName); // Fire and forget
            }

            // Dispose of security validator and health monitor
            (_securityValidator as IDisposable)?.Dispose();
            _healthMonitor.Dispose();
            _lazyPluginLoader.ClearCache(); // Clear the lazy loading cache

            _disposed = true;
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
        var result = new PluginInstallResult();

        try
        {
            // Create temp directory for NuGet download
            var tempDir = Path.Combine(Path.GetTempPath(), $"relay-plugin-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Use dotnet CLI to download the NuGet package
                var versionArg = !string.IsNullOrEmpty(version) ? $" --version {version}" : "";
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"add {tempDir} package {packageName}{versionArg} --package-directory {tempDir}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process == null)
                {
                    result.Success = false;
                    result.Error = "Failed to start dotnet process";
                    return result;
                }

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    result.Success = false;
                    result.Error = $"Failed to download package: {error}";
                    return result;
                }

                // Find the downloaded package directory
                var packageDir = Directory.GetDirectories(tempDir, packageName, SearchOption.AllDirectories).FirstOrDefault();
                if (packageDir == null || !Directory.Exists(packageDir))
                {
                    result.Success = false;
                    result.Error = "Package downloaded but could not locate package directory";
                    return result;
                }

                // Look for plugin.json in the package
                var manifestPath = Directory.GetFiles(packageDir, "plugin.json", SearchOption.AllDirectories).FirstOrDefault();
                if (manifestPath == null)
                {
                    result.Success = false;
                    result.Error = "Package does not contain a valid plugin.json manifest";
                    return result;
                }

                // Install from the package directory
                var pluginSourceDir = Path.GetDirectoryName(manifestPath);
                if (pluginSourceDir == null)
                {
                    result.Success = false;
                    result.Error = "Could not determine plugin source directory";
                    return result;
                }

                result = await InstallFromLocalAsync(pluginSourceDir, global);
            }
            finally
            {
                // Cleanup temp directory
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Error installing from NuGet: {ex.Message}";
        }

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
