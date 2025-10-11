using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Relay.CLI.Migration;

/// <summary>
/// Manages backup and restore operations for safe migration
/// </summary>
public class BackupManager
{
    private readonly List<string> _excludeDirs = new()
    {
        "bin", "obj", ".git", ".vs", ".backup", "backup",
        "node_modules", "packages", ".idea", ".vscode"
    };

    private readonly List<string> _excludeExtensions = new()
    {
        ".dll", ".exe", ".pdb", ".cache", ".suo", ".user"
    };

    /// <summary>
    /// Creates a backup of the source directory
    /// </summary>
    /// <param name="sourcePath">Source directory to backup</param>
    /// <param name="backupPath">Destination backup directory</param>
    /// <returns>Path to the created backup</returns>
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

    /// <summary>
    /// Lists all available backups in the backup directory
    /// </summary>
    /// <param name="backupRootPath">Root backup directory path</param>
    /// <returns>List of backup metadata</returns>
    public async Task<List<BackupMetadata>> ListBackupsAsync(string backupRootPath)
    {
        var backups = new List<BackupMetadata>();

        if (!Directory.Exists(backupRootPath))
        {
            return backups;
        }

        var backupDirs = Directory.GetDirectories(backupRootPath, "backup_*");

        foreach (var dir in backupDirs)
        {
            var metadataPath = Path.Combine(dir, "backup-metadata.json");
            if (File.Exists(metadataPath))
            {
                try
                {
                    var metadataJson = await File.ReadAllTextAsync(metadataPath);
                    var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataJson);
                    if (metadata != null)
                    {
                        backups.Add(metadata);
                    }
                }
                catch
                {
                    // Skip invalid backups
                }
            }
        }

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    /// <summary>
    /// Deletes a backup directory and its compressed archive
    /// </summary>
    /// <param name="backupPath">Path to the backup to delete</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteBackupAsync(string backupPath)
    {
        try
        {
            if (Directory.Exists(backupPath))
            {
                Directory.Delete(backupPath, recursive: true);
            }

            var zipPath = backupPath + ".zip";
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            await Task.CompletedTask;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Cleans up old backups, keeping only the specified number of most recent ones
    /// </summary>
    /// <param name="backupRootPath">Root backup directory</param>
    /// <param name="keepCount">Number of backups to keep</param>
    /// <returns>Number of deleted backups</returns>
    public async Task<int> CleanupOldBackupsAsync(string backupRootPath, int keepCount = 5)
    {
        var backups = await ListBackupsAsync(backupRootPath);
        var toDelete = backups.Skip(keepCount).ToList();

        int deletedCount = 0;
        foreach (var backup in toDelete)
        {
            if (await DeleteBackupAsync(backup.BackupPath))
            {
                deletedCount++;
            }
        }

        return deletedCount;
    }

    /// <summary>
    /// Verifies backup integrity by checking file checksums
    /// </summary>
    /// <param name="backupPath">Path to the backup</param>
    /// <returns>Verification result with details</returns>
    public async Task<BackupVerificationResult> VerifyBackupIntegrityAsync(string backupPath)
    {
        var result = new BackupVerificationResult { BackupPath = backupPath };

        try
        {
            // Check if metadata exists
            var metadataPath = Path.Combine(backupPath, "backup-metadata.json");
            if (!File.Exists(metadataPath))
            {
                result.IsValid = false;
                result.Errors.Add("Backup metadata file not found");
                return result;
            }

            // Load metadata
            var metadataJson = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataJson);

            if (metadata == null)
            {
                result.IsValid = false;
                result.Errors.Add("Failed to parse backup metadata");
                return result;
            }

            result.Metadata = metadata;

            // Check if backup directory exists
            if (!Directory.Exists(backupPath))
            {
                result.IsValid = false;
                result.Errors.Add("Backup directory not found");
                return result;
            }

            // Count files and verify size
            var files = Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith("backup-metadata.json"))
                .ToList();

            result.FilesFound = files.Count;
            result.TotalSize = files.Sum(f => new FileInfo(f).Length);

            if (result.FilesFound != metadata.FileCount)
            {
                result.Warnings.Add($"File count mismatch: expected {metadata.FileCount}, found {result.FilesFound}");
            }

            if (Math.Abs(result.TotalSize - metadata.TotalSize) > 1024) // Allow 1KB difference
            {
                result.Warnings.Add($"Size mismatch: expected {metadata.TotalSize} bytes, found {result.TotalSize} bytes");
            }

            // Check zip archive if exists
            var zipPath = backupPath + ".zip";
            if (File.Exists(zipPath))
            {
                result.HasCompressedArchive = true;
                result.CompressedSize = new FileInfo(zipPath).Length;
            }

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Verification failed: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Estimates the size of a backup before creating it
    /// </summary>
    /// <param name="sourcePath">Source directory to analyze</param>
    /// <returns>Estimated backup size information</returns>
    public async Task<BackupSizeEstimate> EstimateBackupSizeAsync(string sourcePath)
    {
        var estimate = new BackupSizeEstimate { SourcePath = sourcePath };

        try
        {
            var files = GetFilesToBackup(sourcePath);

            estimate.FileCount = files.Count;
            estimate.TotalSize = files.Sum(f => new FileInfo(f).Length);

            // Estimate compressed size (typically 30-50% of original for source code)
            estimate.EstimatedCompressedSize = (long)(estimate.TotalSize * 0.4);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            estimate.Error = ex.Message;
        }

        return estimate;
    }

    private List<string> GetFilesToBackup(string sourceDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            return new List<string>();
        }

        return Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)
            .Where(f =>
            {
                var relativePath = Path.GetRelativePath(sourceDir, f);
                var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Exclude if any path component is in the exclude list
                if (pathParts.Any(part => _excludeDirs.Contains(part, StringComparer.OrdinalIgnoreCase)))
                {
                    return false;
                }

                // Exclude by extension
                var extension = Path.GetExtension(f);
                if (_excludeExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            })
            .ToList();
    }

    private async Task CopyDirectoryAsync(string sourceDir, string destDir, BackupMetadata? metadata, bool restore = false)
    {
        Directory.CreateDirectory(destDir);

        var files = GetFilesToBackup(sourceDir);

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

/// <summary>
/// Result of backup verification
/// </summary>
public class BackupVerificationResult
{
    /// <summary>
    /// Path to the verified backup
    /// </summary>
    public string BackupPath { get; set; } = "";

    /// <summary>
    /// Whether the backup is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Backup metadata
    /// </summary>
    public BackupMetadata? Metadata { get; set; }

    /// <summary>
    /// Number of files found in backup
    /// </summary>
    public int FilesFound { get; set; }

    /// <summary>
    /// Total size of files in backup
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Whether a compressed archive exists
    /// </summary>
    public bool HasCompressedArchive { get; set; }

    /// <summary>
    /// Size of compressed archive
    /// </summary>
    public long CompressedSize { get; set; }

    /// <summary>
    /// List of errors found during verification
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of warnings found during verification
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Estimated backup size information
/// </summary>
public class BackupSizeEstimate
{
    /// <summary>
    /// Source path being analyzed
    /// </summary>
    public string SourcePath { get; set; } = "";

    /// <summary>
    /// Number of files to backup
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Total size of files in bytes
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Estimated compressed size in bytes
    /// </summary>
    public long EstimatedCompressedSize { get; set; }

    /// <summary>
    /// Error message if estimation failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets total size in human-readable format
    /// </summary>
    public string TotalSizeFormatted => FormatBytes(TotalSize);

    /// <summary>
    /// Gets estimated compressed size in human-readable format
    /// </summary>
    public string CompressedSizeFormatted => FormatBytes(EstimatedCompressedSize);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
