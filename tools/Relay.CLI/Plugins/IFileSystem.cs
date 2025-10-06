namespace Relay.CLI.Plugins;

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
