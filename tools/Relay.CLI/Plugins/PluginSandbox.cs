using System.Security;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace Relay.CLI.Plugins;

/// <summary>
/// Provides isolated execution environment for plugins
/// </summary>
public class PluginSandbox : IDisposable
{
    private readonly IPluginLogger _logger;
    private readonly PluginPermissions? _permissions;
    private bool _disposed = false;

    public PluginSandbox(IPluginLogger logger, PluginPermissions? permissions = null)
    {
        _logger = logger;
        _permissions = permissions;
    }

    /// <summary>
    /// Executes a plugin operation in a restricted security context
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<T?> ExecuteInSandboxAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginSandbox));

        _logger.LogDebug("Executing plugin operation in sandbox");

        try
        {
            // Apply permission restrictions if available
            if (_permissions != null)
            {
                ApplyPermissions();
            }

            // Execute the operation with timeout protection
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // Default timeout
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var task = operation();
            var completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completedTask == task)
            {
                return await task;
            }
            else
            {
                _logger.LogWarning("Plugin operation timed out");
                throw new TimeoutException("Plugin operation exceeded timeout limit");
            }
        }
        catch (SecurityException ex)
        {
            _logger.LogError($"Security violation in plugin: {ex.Message}", ex);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Plugin attempted unauthorized access: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error executing plugin in sandbox: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Executes a plugin operation with resource limits
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<T?> ExecuteWithResourceLimitsAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginSandbox));

        _logger.LogDebug("Executing plugin operation with resource limits");

        // In a real implementation, we would set up resource limits here
        // For now, we'll just execute the operation with monitoring
        
        var startTime = DateTime.UtcNow;
        var startMemory = GC.GetTotalMemory(false);

        try
        {
            var result = await operation();

            var endTime = DateTime.UtcNow;
            var endMemory = GC.GetTotalMemory(false);
            
            var executionTime = endTime - startTime;
            var memoryUsed = endMemory - startMemory;

            _logger.LogDebug($"Plugin execution completed in {executionTime.TotalMilliseconds}ms, memory change: {memoryUsed} bytes");

            // Check for resource limits
            if (executionTime.TotalSeconds > 30) // 30 second limit
            {
                _logger.LogWarning($"Plugin execution time exceeded recommended limit: {executionTime.TotalSeconds}s");
            }

            if (memoryUsed > 50 * 1024 * 1024) // 50MB limit
            {
                _logger.LogWarning($"Plugin memory usage exceeded recommended limit: {memoryUsed / (1024 * 1024)}MB");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error executing plugin with resource limits: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Applies security permissions to the current execution context
    /// </summary>
    private void ApplyPermissions()
    {
        if (_permissions == null) return;

        // In a full implementation, we would apply actual .NET security permissions
        // Since .NET Core doesn't have Code Access Security (CAS), we'll implement custom checks
        // The actual permission enforcement will be done in the IFileSystem and other interfaces
    }

    /// <summary>
    /// Validates file system access based on permissions
    /// </summary>
    /// <param name="path">File path to validate</param>
    /// <param name="accessType">Type of access requested</param>
    /// <returns>True if access is allowed, false otherwise</returns>
    public bool ValidateFileSystemAccess(string path, FileSystemAccessType accessType)
    {
        if (_permissions?.FileSystem == null)
        {
            return false; // No permissions defined, deny access
        }

        var fsPermissions = _permissions.FileSystem;

        // Check if access type is allowed
        switch (accessType)
        {
            case FileSystemAccessType.Read when !fsPermissions.Read:
            case FileSystemAccessType.Write when !fsPermissions.Write:
            case FileSystemAccessType.Delete when !fsPermissions.Delete:
                return false;
        }

        // Check if path is in allowed list
        if (fsPermissions.AllowedPaths?.Length > 0)
        {
            var isAllowed = false;
            foreach (var allowedPath in fsPermissions.AllowedPaths)
            {
                var fullPath = Path.GetFullPath(path);
                var allowedFullPath = Path.GetFullPath(allowedPath);
                
                if (fullPath.StartsWith(allowedFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    isAllowed = true;
                    break;
                }
            }
            
            if (!isAllowed) return false;
        }

        // Check if path is in denied list
        if (fsPermissions.DeniedPaths?.Length > 0)
        {
            foreach (var deniedPath in fsPermissions.DeniedPaths)
            {
                var fullPath = Path.GetFullPath(path);
                var deniedFullPath = Path.GetFullPath(deniedPath);
                
                if (fullPath.StartsWith(deniedFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Validates network access based on permissions
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <returns>True if access is allowed, false otherwise</returns>
    public bool ValidateNetworkAccess(string url)
    {
        if (_permissions?.Network == null)
        {
            return false; // No permissions defined, deny access
        }

        var netPermissions = _permissions.Network;

        // Check if HTTP/HTTPS is allowed
        if (url.StartsWith("http://") && !netPermissions.Http)
        {
            return false;
        }
        
        if (url.StartsWith("https://") && !netPermissions.Https)
        {
            return false;
        }

        // Extract host from URL
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;

            // Check if host is in allowed list
            if (netPermissions.AllowedHosts?.Length > 0)
            {
                var isAllowed = false;
                foreach (var allowedHost in netPermissions.AllowedHosts)
                {
                    if (host.Equals(allowedHost, StringComparison.OrdinalIgnoreCase))
                    {
                        isAllowed = true;
                        break;
                    }
                }
                
                if (!isAllowed) return false;
            }

            // Check if host is in denied list
            if (netPermissions.DeniedHosts?.Length > 0)
            {
                foreach (var deniedHost in netPermissions.DeniedHosts)
                {
                    if (host.Equals(deniedHost, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Type of file system access
/// </summary>
public enum FileSystemAccessType
{
    Read,
    Write,
    Delete
}