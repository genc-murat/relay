using System.IO.Compression;
using System.Text.Json;

namespace Relay.CLI.Migration;

/// <summary>
/// Manages backup and restore operations for safe migration
/// </summary>
public class BackupManager
{
    public async Task<string> CreateBackupAsync(string sourcePath, string backupPath)
    {
        Directory.CreateDirectory(backupPath);

        var metadata = new BackupMetadata
        {
            BackupId = Guid.NewGuid().ToString(),
            SourcePath = sourcePath,
            BackupPath = backupPath,
            CreatedAt = DateTime.UtcNow,
            ToolVersion = "2.1.0"
        };

        // Copy all relevant files
        await CopyDirectoryAsync(sourcePath, backupPath, metadata);

        // Save metadata
        var metadataPath = Path.Combine(backupPath, "backup-metadata.json");
        var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, metadataJson);

        // Create compressed archive (optional)
        var zipPath = backupPath + ".zip";
        ZipFile.CreateFromDirectory(backupPath, zipPath);

        return backupPath;
    }

    public async Task<bool> RestoreBackupAsync(string backupPath)
    {
        try
        {
            // Load metadata
            var metadataPath = Path.Combine(backupPath, "backup-metadata.json");
            if (!File.Exists(metadataPath))
            {
                return false;
            }

            var metadataJson = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataJson);

            if (metadata == null || string.IsNullOrEmpty(metadata.SourcePath))
            {
                return false;
            }

            // Restore files
            await CopyDirectoryAsync(backupPath, metadata.SourcePath, null, restore: true);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> VerifyBackupAsync(string backupPath)
    {
        try
        {
            var metadataPath = Path.Combine(backupPath, "backup-metadata.json");
            if (!File.Exists(metadataPath))
            {
                return false;
            }

            var metadataJson = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataJson);

            return metadata != null && 
                   metadata.FileCount > 0 && 
                   Directory.Exists(backupPath);
        }
        catch
        {
            return false;
        }
    }

    private async Task CopyDirectoryAsync(string sourceDir, string destDir, BackupMetadata? metadata, bool restore = false)
    {
        Directory.CreateDirectory(destDir);

        // Directories to exclude
        var excludeDirs = new[] { "bin", "obj", ".git", ".vs", "backup", "node_modules", "packages" };

        // Get files to copy
        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)
            .Where(f => 
            {
                var relativePath = Path.GetRelativePath(sourceDir, f);
                var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                
                // Exclude if any path component is in the exclude list
                return !pathParts.Any(part => excludeDirs.Contains(part, StringComparer.OrdinalIgnoreCase));
            })
            .ToList();

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);

            // Create directory if needed
            var destFileDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destFileDir))
            {
                Directory.CreateDirectory(destFileDir);
            }

            // Copy file
            File.Copy(file, destFile, overwrite: restore);

            if (metadata != null)
            {
                metadata.FileCount++;
                metadata.TotalSize += new FileInfo(file).Length;
            }
        }

        await Task.CompletedTask;
    }
}

public class BackupMetadata
{
    public string BackupId { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string BackupPath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string ToolVersion { get; set; } = "";
    public int FileCount { get; set; }
    public long TotalSize { get; set; }
}
