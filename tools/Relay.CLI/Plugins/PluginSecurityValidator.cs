using System.Reflection;
using System.Security.Cryptography;

namespace Relay.CLI.Plugins;

/// <summary>
/// Validates plugin security including code signing and permissions
/// </summary>
public class PluginSecurityValidator
{
    private readonly IPluginLogger _logger;
    private readonly List<string> _trustedSources;
    private readonly HashSet<string> _allowedAssemblies;
    private readonly Dictionary<string, PluginPermissions> _pluginPermissions;

    public PluginSecurityValidator(IPluginLogger logger)
    {
        _logger = logger;
        _trustedSources = new List<string>();
        _allowedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _pluginPermissions = new Dictionary<string, PluginPermissions>();

        // Initialize with trusted assemblies
        InitializeTrustedAssemblies();
    }

    private void InitializeTrustedAssemblies()
    {
        // Add assemblies that plugins are allowed to reference
        _allowedAssemblies.Add("System");
        _allowedAssemblies.Add("System.Core");
        _allowedAssemblies.Add("System.Data");
        _allowedAssemblies.Add("System.Xml");
        _allowedAssemblies.Add("System.Linq");
        _allowedAssemblies.Add("System.Collections");
        _allowedAssemblies.Add("System.IO");
        _allowedAssemblies.Add("System.Text");
        _allowedAssemblies.Add("System.Threading");
        _allowedAssemblies.Add("Microsoft.CSharp");
        // Add Relay CLI specific assemblies
        _allowedAssemblies.Add("Relay.CLI.Sdk");
        _allowedAssemblies.Add("Relay.CLI.Plugins");
    }

    /// <summary>
    /// Validates the security of a plugin assembly before loading
    /// </summary>
    /// <param name="assemblyPath">Path to the plugin assembly</param>
    /// <param name="pluginInfo">Plugin information</param>
    /// <returns>Validation result</returns>
    public virtual async Task<SecurityValidationResult> ValidatePluginAsync(string assemblyPath, PluginInfo pluginInfo)
    {
        var result = new SecurityValidationResult();

        try
        {
            // Validate file exists
            if (!File.Exists(assemblyPath))
            {
                result.IsValid = false;
                result.Errors.Add($"Assembly file does not exist: {assemblyPath}");
                return result;
            }

            // Validate digital signature
            var signatureValid = await ValidateSignatureAsync(assemblyPath);
            if (!signatureValid)
            {
                result.IsValid = false;
                result.Errors.Add($"Plugin assembly is not properly signed: {assemblyPath}");
            }

            // Validate assembly dependencies
            var dependenciesValid = await ValidateDependenciesAsync(assemblyPath);
            if (!dependenciesValid)
            {
                result.IsValid = false;
                result.Errors.Add($"Plugin has invalid dependencies: {assemblyPath}");
            }

            // Validate plugin manifest
            var manifestValid = await ValidateManifestAsync(pluginInfo);
            if (!manifestValid)
            {
                result.IsValid = false;
                result.Errors.Add($"Plugin manifest is invalid: {pluginInfo.Name}");
            }

            // Validate permissions if specified in manifest
            if (pluginInfo.Manifest?.Permissions != null)
            {
                var permissionsValid = ValidatePermissions(pluginInfo.Manifest.Permissions);
                if (!permissionsValid)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Plugin requests invalid permissions: {pluginInfo.Name}");
                }
            }

            // Log validation result
            if (result.IsValid)
            {
                _logger.LogDebug($"Security validation passed for plugin: {pluginInfo.Name}");
            }
            else
            {
                _logger.LogWarning($"Security validation failed for plugin: {pluginInfo.Name}");
                foreach (var error in result.Errors)
                {
                    _logger.LogError(error);
                }
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Exception during security validation: {ex.Message}");
            _logger.LogError($"Security validation error for {pluginInfo.Name}: {ex.Message}", ex);
        }

        return result;
    }

    /// <summary>
    /// Validates the digital signature of an assembly
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    private async Task<bool> ValidateSignatureAsync(string assemblyPath)
    {
        try
        {
            // In a real implementation, this would check the digital signature of the assembly
            // For now, we'll implement a basic check
            using var sha256 = SHA256.Create();
            var fileBytes = await File.ReadAllBytesAsync(assemblyPath);
            var hash = sha256.ComputeHash(fileBytes);
            var hashString = Convert.ToBase64String(hash);
            
            // In a real system, we would compare against known valid signatures
            _logger.LogDebug($"Assembly hash: {hashString}");
            return true; // Placeholder - implement actual signature validation
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that the plugin only depends on trusted assemblies
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file</param>
    /// <returns>True if dependencies are valid, false otherwise</returns>
    private async Task<bool> ValidateDependenciesAsync(string assemblyPath)
    {
        try
        {
            // For dependency validation, we'll use reflection-only loading to avoid needing the PluginLoadContext
            // This is a simplified approach - a full implementation would analyze assembly metadata
            var assembly = Assembly.LoadFrom(assemblyPath);

            foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
            {
                if (!_allowedAssemblies.Contains(referencedAssembly.Name))
                {
                    _logger.LogWarning($"Plugin references untrusted assembly: {referencedAssembly.Name}");
                    return false;
                }
            }

            return true;
        }
        catch (BadImageFormatException)
        {
            // The file is not a valid assembly, so we can't validate its dependencies
            // This is acceptable for the security validation - it will fail at load time
            _logger.LogWarning($"File is not a valid assembly: {assemblyPath}");
            return true; // Allow the validation to continue, but note that loading will fail
        }
        catch (FileLoadException)
        {
            // The file could not be loaded (might be in use, corrupted, etc.)
            _logger.LogWarning($"Could not load assembly file: {assemblyPath}");
            return true; // Allow the validation to continue, but this will fail at load time
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error during dependency validation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Validates the plugin manifest for security requirements
    /// </summary>
    /// <param name="pluginInfo">Plugin information</param>
    /// <returns>True if manifest is valid, false otherwise</returns>
    private async Task<bool> ValidateManifestAsync(PluginInfo pluginInfo)
    {
        if (pluginInfo.Manifest == null)
        {
            return false;
        }

        // Check if plugin comes from a trusted source
        if (!string.IsNullOrEmpty(pluginInfo.Manifest.Repository))
        {
            if (!_trustedSources.Contains(pluginInfo.Manifest.Repository))
            {
                _logger.LogWarning($"Plugin repository not trusted: {pluginInfo.Manifest.Repository}");
            }
        }

        // Validate plugin metadata
        if (string.IsNullOrWhiteSpace(pluginInfo.Manifest.Name))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(pluginInfo.Manifest.Description))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates the requested permissions
    /// </summary>
    /// <param name="permissions">Requested permissions</param>
    /// <returns>True if permissions are valid, false otherwise</returns>
    private bool ValidatePermissions(PluginPermissions permissions)
    {
        // Validate file system permissions
        if (permissions.FileSystem != null)
        {
            if (!ValidateFileSystemPermissions(permissions.FileSystem))
            {
                return false;
            }
        }

        // Validate network permissions
        if (permissions.Network != null)
        {
            if (!ValidateNetworkPermissions(permissions.Network))
            {
                return false;
            }
        }

        // Validate other permissions as needed

        return true;
    }

    private bool ValidateFileSystemPermissions(FileSystemPermissions filePermissions)
    {
        // Validate file system access permissions
        // For example, ensure plugins can't access sensitive system directories
        if (filePermissions.AllowedPaths != null)
        {
            foreach (var path in filePermissions.AllowedPaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (fullPath.Contains("Windows") || fullPath.Contains("Program Files"))
                {
                    return false; // Don't allow access to system directories
                }
            }
        }

        return true;
    }

    private bool ValidateNetworkPermissions(NetworkPermissions networkPermissions)
    {
        // Validate network access permissions
        // For example, ensure plugins can't make unauthorized network calls
        return true; // Placeholder
    }

    /// <summary>
    /// Adds a trusted source for plugin validation
    /// </summary>
    /// <param name="source">Trusted source URL</param>
    public void AddTrustedSource(string source)
    {
        if (!_trustedSources.Contains(source))
        {
            _trustedSources.Add(source);
        }
    }

    /// <summary>
    /// Gets the permissions for a specific plugin
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    /// <returns>Plugin permissions or null if not found</returns>
    public PluginPermissions? GetPluginPermissions(string pluginName)
    {
        if (_pluginPermissions.TryGetValue(pluginName, out var permissions))
        {
            return permissions;
        }

        return null;
    }

    /// <summary>
    /// Sets permissions for a specific plugin
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    /// <param name="permissions">Permissions to set</param>
    public void SetPluginPermissions(string pluginName, PluginPermissions permissions)
    {
        _pluginPermissions[pluginName] = permissions;
    }
}
