using System.Reflection;
using System.Runtime.Loader;

namespace Relay.CLI.Plugins;

/// <summary>
/// Enhanced dependency resolver for plugin assemblies
/// </summary>
public class DependencyResolver
{
    private readonly IPluginLogger _logger;
    private readonly Dictionary<string, Assembly> _sharedAssemblies;
    private readonly Dictionary<string, List<string>> _versionConflicts;

    public DependencyResolver(IPluginLogger logger)
    {
        _logger = logger;
        _sharedAssemblies = new Dictionary<string, Assembly>();
        _versionConflicts = new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Resolves dependencies for a plugin assembly with conflict detection and resolution
    /// </summary>
    /// <param name="pluginPath">Path to the plugin assembly</param>
    /// <returns>Resolved dependency paths</returns>
    public async Task<List<string>> ResolveDependenciesAsync(string pluginPath)
    {
        var resolvedDependencies = new List<string>();
        
        try
        {
            var resolver = new AssemblyDependencyResolver(pluginPath);
            var assemblyName = Path.GetFileNameWithoutExtension(pluginPath);
            
            // Load the main plugin assembly to analyze its references
            Assembly mainAssembly;
            try
            {
                mainAssembly = Assembly.LoadFrom(pluginPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load plugin assembly for dependency analysis: {ex.Message}", ex);
                return resolvedDependencies;
            }

            // Get all referenced assemblies
            var referencedAssemblies = mainAssembly.GetReferencedAssemblies();
            
            foreach (var reference in referencedAssemblies)
            {
                var assemblyPath = resolver.ResolveAssemblyToPath(reference);
                
                if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
                {
                    // Check for version conflicts
                    var conflict = await CheckVersionConflictAsync(reference, assemblyPath);
                    if (conflict != null)
                    {
                        _logger.LogWarning($"Version conflict detected for {reference.Name}: {conflict}");
                        
                        // Attempt to resolve conflict
                        var resolvedPath = await ResolveVersionConflictAsync(reference, assemblyPath);
                        if (!string.IsNullOrEmpty(resolvedPath))
                        {
                            resolvedDependencies.Add(resolvedPath);
                        }
                    }
                    else
                    {
                        resolvedDependencies.Add(assemblyPath);
                    }
                    
                    // Register shared assembly if not already registered
                    if (reference.Name != null)
                    {
                        RegisterSharedAssembly(reference.Name, assemblyPath);
                    }
                }
            }
            
            _logger.LogDebug($"Resolved {resolvedDependencies.Count} dependencies for plugin: {assemblyName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resolving dependencies for {pluginPath}: {ex.Message}", ex);
        }

        return resolvedDependencies;
    }

    /// <summary>
    /// Registers a shared assembly that can be used by multiple plugins
    /// </summary>
    /// <param name="assemblyName">Name of the assembly</param>
    /// <param name="assemblyPath">Path to the assembly</param>
    public void RegisterSharedAssembly(string assemblyName, string assemblyPath)
    {
        if (!_sharedAssemblies.ContainsKey(assemblyName))
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                _sharedAssemblies[assemblyName] = assembly;
                _logger.LogDebug($"Registered shared assembly: {assemblyName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to register shared assembly {assemblyName}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets a shared assembly if available
    /// </summary>
    /// <param name="assemblyName">Name of the assembly to get</param>
    /// <returns>The assembly if found, null otherwise</returns>
    public Assembly? GetSharedAssembly(string assemblyName)
    {
        _sharedAssemblies.TryGetValue(assemblyName, out var assembly);
        return assembly;
    }

    /// <summary>
    /// Gets all registered shared assemblies
    /// </summary>
    /// <returns>Dictionary of shared assemblies</returns>
    public Dictionary<string, Assembly> GetSharedAssemblies()
    {
        return new Dictionary<string, Assembly>(_sharedAssemblies);
    }

    /// <summary>
    /// Checks for version conflicts between assemblies
    /// </summary>
    /// <param name="reference">Assembly reference to check</param>
    /// <param name="assemblyPath">Path to the assembly</param>
    /// <returns>Conflict description if found, null otherwise</returns>
    private async Task<string?> CheckVersionConflictAsync(AssemblyName reference, string assemblyPath)
    {
        var assemblyDir = Path.GetDirectoryName(assemblyPath);
        if (string.IsNullOrEmpty(assemblyDir) || !Directory.Exists(assemblyDir))
        {
            return null;
        }

        // Look for other versions of the same assembly in the plugin directory
        var assemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyPath);
        var files = Directory.GetFiles(assemblyDir, $"{assemblyNameWithoutExtension}*.dll");

        foreach (var file in files)
        {
            if (Path.GetFileName(file) != Path.GetFileName(assemblyPath))
            {
                try
                {
                    var otherAssembly = AssemblyName.GetAssemblyName(file);
                    if (otherAssembly.Name == reference.Name && otherAssembly.Version != reference.Version)
                    {
                        var conflict = $"Version conflict: {reference.Name} {reference.Version} vs {otherAssembly.Version}";
                        if (reference.Name != null && !_versionConflicts.ContainsKey(reference.Name))
                        {
                            _versionConflicts[reference.Name] = new List<string>();
                        }
                        if (reference.Name != null)
                        {
                            _versionConflicts[reference.Name].Add(conflict);
                        }
                        return conflict;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not compare assembly {file} for version conflicts: {ex.Message}");
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to resolve version conflicts by selecting the most appropriate version
    /// </summary>
    /// <param name="reference">Assembly reference</param>
    /// <param name="assemblyPath">Original assembly path</param>
    /// <returns>Resolved assembly path</returns>
    private async Task<string?> ResolveVersionConflictAsync(AssemblyName reference, string assemblyPath)
    {
        // For now, implement a basic strategy: prefer higher versions when possible
        // In a more sophisticated implementation, we could use semantic versioning rules
        var assemblyDir = Path.GetDirectoryName(assemblyPath);
        if (string.IsNullOrEmpty(assemblyDir) || !Directory.Exists(assemblyDir))
        {
            return assemblyPath; // Return original if we can't resolve
        }

        var assemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyPath);
        var pattern = $"{assemblyNameWithoutExtension}*.dll";
        var files = Directory.GetFiles(assemblyDir, pattern);

        AssemblyName? bestMatch = null;
        string? bestPath = null;
        Version? bestVersion = null;

        foreach (var file in files)
        {
            try
            {
                var otherAssembly = AssemblyName.GetAssemblyName(file);
                if (otherAssembly.Name == reference.Name)
                {
                    var version = otherAssembly.Version ?? new Version(0, 0, 0, 0);
                    
                    // Prefer higher versions
                    if (bestVersion == null || version > bestVersion)
                    {
                        bestVersion = version;
                        bestMatch = otherAssembly;
                        bestPath = file;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not evaluate assembly {file} for resolution: {ex.Message}");
            }
        }

        if (bestPath != null)
        {
            _logger.LogInformation($"Selected version {bestVersion} for {reference.Name}");
            return bestPath;
        }

        // If no better version found, use the original
        return assemblyPath;
    }

    /// <summary>
    /// Gets all detected version conflicts
    /// </summary>
    /// <returns>Dictionary of version conflicts</returns>
    public Dictionary<string, List<string>> GetVersionConflicts()
    {
        return new Dictionary<string, List<string>>(_versionConflicts);
    }

    /// <summary>
    /// Clears all cached dependency information
    /// </summary>
    public void ClearCache()
    {
        _sharedAssemblies.Clear();
        _versionConflicts.Clear();
    }

    /// <summary>
    /// Validates that all dependencies for an assembly can be resolved
    /// </summary>
    /// <param name="pluginPath">Path to the plugin assembly</param>
    /// <returns>True if all dependencies can be resolved, false otherwise</returns>
    public async Task<bool> ValidateDependenciesAsync(string pluginPath)
    {
        try
        {
            var resolver = new AssemblyDependencyResolver(pluginPath);
            var assemblyName = Path.GetFileNameWithoutExtension(pluginPath);
            
            // Load the main plugin assembly to analyze its references
            Assembly mainAssembly;
            try
            {
                mainAssembly = Assembly.LoadFrom(pluginPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load plugin assembly for validation: {ex.Message}", ex);
                return false;
            }

            var referencedAssemblies = mainAssembly.GetReferencedAssemblies();
            
            foreach (var reference in referencedAssemblies)
            {
                var assemblyPath = resolver.ResolveAssemblyToPath(reference);
                
                if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
                {
                    _logger.LogWarning($"Dependency not found: {reference.FullName}");
                    return false;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error validating dependencies for {pluginPath}: {ex.Message}", ex);
            return false;
        }
    }
}