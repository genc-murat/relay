namespace Relay.CLI.Plugins;

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
