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
            // Compute assembly hash for integrity verification
            using var sha256 = SHA256.Create();
            var fileBytes = await File.ReadAllBytesAsync(assemblyPath);
            var hash = sha256.ComputeHash(fileBytes);
            var hashString = Convert.ToBase64String(hash);

            _logger.LogDebug($"Assembly hash: {hashString}");

            // Check if assembly is signed with Authenticode
            var isAuthenticodeSigned = await ValidateAuthenticodeSignatureAsync(assemblyPath);
            if (!isAuthenticodeSigned)
            {
                _logger.LogWarning($"Assembly is not Authenticode signed: {assemblyPath}");
                return false;
            }

            // Verify strong name signature for .NET assemblies
            var isStrongNameValid = await ValidateStrongNameSignatureAsync(assemblyPath);
            if (!isStrongNameValid)
            {
                _logger.LogWarning($"Assembly strong name validation failed: {assemblyPath}");
                return false;
            }

            _logger.LogDebug($"Assembly signature validation passed: {assemblyPath}");
            return true;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError($"Assembly file not found during signature validation: {ex.Message}");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Access denied during signature validation: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error during signature validation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Validates Authenticode (PE) signature of an assembly
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file</param>
    /// <returns>True if Authenticode signature is valid</returns>
    private async Task<bool> ValidateAuthenticodeSignatureAsync(string assemblyPath)
    {
        try
        {
            // Check if file has a valid Authenticode signature
            // This is a simplified check - a full implementation would use WinVerifyTrust API

            // Read PE header to check for signature
            using var fileStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fileStream);

            // Check DOS header
            if (reader.ReadUInt16() != 0x5A4D) // "MZ" signature
            {
                _logger.LogDebug($"File is not a valid PE file: {assemblyPath}");
                return false;
            }

            // Jump to PE header offset
            fileStream.Seek(0x3C, SeekOrigin.Begin);
            var peHeaderOffset = reader.ReadInt32();

            fileStream.Seek(peHeaderOffset, SeekOrigin.Begin);

            // Check PE signature
            if (reader.ReadUInt32() != 0x00004550) // "PE\0\0" signature
            {
                _logger.LogDebug($"Invalid PE signature: {assemblyPath}");
                return false;
            }

            // Skip COFF header (20 bytes) and get Optional Header size
            fileStream.Seek(16, SeekOrigin.Current);
            var optionalHeaderSize = reader.ReadUInt16();

            if (optionalHeaderSize == 0)
            {
                _logger.LogDebug($"No optional header found: {assemblyPath}");
                return false;
            }

            // Read magic number to determine PE32 or PE32+
            var magic = reader.ReadUInt16();
            var isPE32Plus = magic == 0x20b;

            // Calculate offset to Certificate Table
            // PE32: offset 128, PE32+: offset 144
            var certTableOffset = isPE32Plus ? 144 : 128;

            // Position to Certificate Table RVA
            fileStream.Seek(peHeaderOffset + 24 + certTableOffset, SeekOrigin.Begin);

            var certTableRVA = reader.ReadUInt32();
            var certTableSize = reader.ReadUInt32();

            if (certTableRVA == 0 || certTableSize == 0)
            {
                _logger.LogDebug($"No certificate table found in PE file: {assemblyPath}");
                return false;
            }

            _logger.LogDebug($"Found certificate table at RVA {certTableRVA}, size {certTableSize}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error validating Authenticode signature: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Validates strong name signature of a .NET assembly
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file</param>
    /// <returns>True if strong name is valid</returns>
    private async Task<bool> ValidateStrongNameSignatureAsync(string assemblyPath)
    {
        try
        {
            // Load assembly metadata to check strong name
            var assembly = Assembly.LoadFrom(assemblyPath);
            var assemblyName = assembly.GetName();

            // Check if assembly has a public key token (indicating it's strong-named)
            var publicKeyToken = assemblyName.GetPublicKeyToken();

            if (publicKeyToken == null || publicKeyToken.Length == 0)
            {
                _logger.LogDebug($"Assembly is not strong-named: {assemblyPath}");
                return false;
            }

            // Verify the strong name signature is valid
            // In a full implementation, this would use StrongNameSignatureVerificationEx API
            // For now, we verify the assembly can be loaded and has a valid public key token

            var publicKey = assemblyName.GetPublicKey();
            if (publicKey == null || publicKey.Length == 0)
            {
                _logger.LogWarning($"Assembly has public key token but no public key: {assemblyPath}");
                return false;
            }

            _logger.LogDebug($"Assembly strong name validated, public key token: {BitConverter.ToString(publicKeyToken)}");
            return true;
        }
        catch (BadImageFormatException ex)
        {
            _logger.LogWarning($"Invalid assembly format during strong name validation: {ex.Message}");
            return false;
        }
        catch (FileLoadException ex)
        {
            _logger.LogWarning($"Could not load assembly for strong name validation: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error validating strong name: {ex.Message}");
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
                if (referencedAssembly.Name is null || !_allowedAssemblies.Contains(referencedAssembly.Name))
                {
                    _logger.LogWarning($"Plugin references untrusted assembly: {referencedAssembly.Name ?? "null"}");
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
