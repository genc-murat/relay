namespace Relay.CLI.Plugins;

/// <summary>
/// File system implementation with security validation
/// </summary>
public class PluginFileSystem : IFileSystem
{
    private readonly IPluginLogger _logger;
    private readonly PluginSandbox? _sandbox;

    public PluginFileSystem(IPluginLogger logger, PluginSandbox? sandbox = null)
    {
        _logger = logger;
        _sandbox = sandbox;
    }

    public async Task<bool> FileExistsAsync(string path)
    {
        if (_sandbox != null && !_sandbox.ValidateFileSystemAccess(path, FileSystemAccessType.Read))
        {
            _logger.LogError($"Plugin attempted unauthorized file access: {path}");
            throw new UnauthorizedAccessException($"Access denied to path: {path}");
        }

        return await Task.FromResult(File.Exists(path));
    }

    public async Task<bool> DirectoryExistsAsync(string path)
    {
        if (_sandbox != null && !_sandbox.ValidateFileSystemAccess(path, FileSystemAccessType.Read))
        {
            _logger.LogError($"Plugin attempted unauthorized directory access: {path}");
            throw new UnauthorizedAccessException($"Access denied to path: {path}");
        }

        return await Task.FromResult(Directory.Exists(path));
    }

    public async Task<string> ReadFileAsync(string path)
    {
        if (_sandbox != null && !_sandbox.ValidateFileSystemAccess(path, FileSystemAccessType.Read))
        {
            _logger.LogError($"Plugin attempted unauthorized file read: {path}");
            throw new UnauthorizedAccessException($"Access denied to path: {path}");
        }

        return await File.ReadAllTextAsync(path);
    }

    public async Task WriteFileAsync(string path, string content)
    {
        if (_sandbox != null && !_sandbox.ValidateFileSystemAccess(path, FileSystemAccessType.Write))
        {
            _logger.LogError($"Plugin attempted unauthorized file write: {path}");
            throw new UnauthorizedAccessException($"Access denied to path: {path}");
        }

        await File.WriteAllTextAsync(path, content);
    }

    public async Task<string[]> GetFilesAsync(string path, string pattern, bool recursive = false)
    {
        if (_sandbox != null && !_sandbox.ValidateFileSystemAccess(path, FileSystemAccessType.Read))
        {
            _logger.LogError($"Plugin attempted unauthorized directory access: {path}");
            throw new UnauthorizedAccessException($"Access denied to path: {path}");
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return await Task.FromResult(Directory.GetFiles(path, pattern, searchOption));
    }

    public async Task<string[]> GetDirectoriesAsync(string path)
    {
        if (_sandbox != null && !_sandbox.ValidateFileSystemAccess(path, FileSystemAccessType.Read))
        {
            _logger.LogError($"Plugin attempted unauthorized directory access: {path}");
            throw new UnauthorizedAccessException($"Access denied to path: {path}");
        }

        return await Task.FromResult(Directory.GetDirectories(path));
    }

    public async Task CreateDirectoryAsync(string path)
    {
        if (_sandbox != null && !_sandbox.ValidateFileSystemAccess(path, FileSystemAccessType.Write))
        {
            _logger.LogError($"Plugin attempted unauthorized directory creation: {path}");
            throw new UnauthorizedAccessException($"Access denied to path: {path}");
        }

        Directory.CreateDirectory(path);
        await Task.CompletedTask;
    }

    public async Task DeleteFileAsync(string path)
    {
        if (_sandbox != null && !_sandbox.ValidateFileSystemAccess(path, FileSystemAccessType.Delete))
        {
            _logger.LogError($"Plugin attempted unauthorized file deletion: {path}");
            throw new UnauthorizedAccessException($"Access denied to path: {path}");
        }

        File.Delete(path);
        await Task.CompletedTask;
    }

    public async Task DeleteDirectoryAsync(string path, bool recursive = false)
    {
        if (_sandbox != null && !_sandbox.ValidateFileSystemAccess(path, FileSystemAccessType.Delete))
        {
            _logger.LogError($"Plugin attempted unauthorized directory deletion: {path}");
            throw new UnauthorizedAccessException($"Access denied to path: {path}");
        }

        Directory.Delete(path, recursive);
        await Task.CompletedTask;
    }

    public async Task CopyFileAsync(string source, string destination)
    {
        // Validate both source and destination permissions
        if (_sandbox != null)
        {
            if (!_sandbox.ValidateFileSystemAccess(source, FileSystemAccessType.Read))
            {
                _logger.LogError($"Plugin attempted unauthorized file read during copy: {source}");
                throw new UnauthorizedAccessException($"Access denied to source path: {source}");
            }

            if (!_sandbox.ValidateFileSystemAccess(destination, FileSystemAccessType.Write))
            {
                _logger.LogError($"Plugin attempted unauthorized file write during copy: {destination}");
                throw new UnauthorizedAccessException($"Access denied to destination path: {destination}");
            }
        }

        File.Copy(source, destination, true);
        await Task.CompletedTask;
    }

    public async Task MoveFileAsync(string source, string destination)
    {
        // Validate both source and destination permissions
        if (_sandbox != null)
        {
            if (!_sandbox.ValidateFileSystemAccess(source, FileSystemAccessType.Write)) // Moving requires write to source
            {
                _logger.LogError($"Plugin attempted unauthorized file move from: {source}");
                throw new UnauthorizedAccessException($"Access denied to source path: {source}");
            }

            if (!_sandbox.ValidateFileSystemAccess(destination, FileSystemAccessType.Write))
            {
                _logger.LogError($"Plugin attempted unauthorized file move to: {destination}");
                throw new UnauthorizedAccessException($"Access denied to destination path: {destination}");
            }
        }

        File.Move(source, destination, true);
        await Task.CompletedTask;
    }
}
