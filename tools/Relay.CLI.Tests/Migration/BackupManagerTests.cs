using Relay.CLI.Migration;
using System.Text.Json;

namespace Relay.CLI.Tests.Migration;

public class BackupManagerTests : IDisposable
{
    private readonly BackupManager _backupManager;
    private readonly string _testSourcePath;
    private readonly string _testBackupPath;

    public BackupManagerTests()
    {
        _backupManager = new BackupManager();
        _testSourcePath = Path.Combine(Path.GetTempPath(), $"relay-source-{Guid.NewGuid()}");
        _testBackupPath = Path.Combine(Path.GetTempPath(), $"relay-backup-{Guid.NewGuid()}");
        
        Directory.CreateDirectory(_testSourcePath);
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesBackupDirectory()
    {
        // Arrange
        await CreateTestFiles();

        // Act
        var backupPath = await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Assert
        Assert.True(Directory.Exists(backupPath));
        Assert.Equal(_testBackupPath, backupPath);
    }

    [Fact]
    public async Task CreateBackupAsync_CopiesAllFiles()
    {
        // Arrange
        await CreateTestFiles();

        // Act
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Assert
        Assert.True(File.Exists(Path.Combine(_testBackupPath, "test.cs")));
        Assert.True(File.Exists(Path.Combine(_testBackupPath, "test.csproj")));
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesMetadataFile()
    {
        // Arrange
        await CreateTestFiles();

        // Act
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Assert
        var metadataPath = Path.Combine(_testBackupPath, "backup-metadata.json");
        Assert.True(File.Exists(metadataPath));

        var content = await File.ReadAllTextAsync(metadataPath);
        Assert.Contains("BackupId", content);
        Assert.Contains("SourcePath", content);
        Assert.Contains("FileCount", content);
    }

    [Fact]
    public async Task CreateBackupAsync_ExcludesBinAndObjFolders()
    {
        // Arrange
        await CreateTestFiles();
        
        var binPath = Path.Combine(_testSourcePath, "bin");
        var objPath = Path.Combine(_testSourcePath, "obj");
        Directory.CreateDirectory(binPath);
        Directory.CreateDirectory(objPath);
        await File.WriteAllTextAsync(Path.Combine(binPath, "test.dll"), "binary");
        await File.WriteAllTextAsync(Path.Combine(objPath, "test.obj"), "object");

        // Act
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Assert
        Assert.False(Directory.Exists(Path.Combine(_testBackupPath, "bin")));
        Assert.False(Directory.Exists(Path.Combine(_testBackupPath, "obj")));
    }

    [Fact]
    public async Task RestoreBackupAsync_RestoresAllFiles()
    {
        // Arrange
        await CreateTestFiles();
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Delete source files
        Directory.Delete(_testSourcePath, true);
        Directory.CreateDirectory(_testSourcePath);

        // Act
        var restored = await _backupManager.RestoreBackupAsync(_testBackupPath);

        // Assert
        Assert.True(restored);
        Assert.True(File.Exists(Path.Combine(_testSourcePath, "test.cs")));
        Assert.True(File.Exists(Path.Combine(_testSourcePath, "test.csproj")));
    }

    [Fact]
    public async Task RestoreBackupAsync_WithEmptySourcePath_ReturnsFalse()
    {
        // Arrange
        Directory.CreateDirectory(_testBackupPath);
        var metadataPath = Path.Combine(_testBackupPath, "backup-metadata.json");
        var metadata = new BackupMetadata { SourcePath = "" };
        var json = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(metadataPath, json);

        // Act
        var restored = await _backupManager.RestoreBackupAsync(_testBackupPath);

        // Assert
        Assert.False(restored);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithInvalidMetadataJson_ReturnsFalse()
    {
        // Arrange
        Directory.CreateDirectory(_testBackupPath);
        var metadataPath = Path.Combine(_testBackupPath, "backup-metadata.json");
        await File.WriteAllTextAsync(metadataPath, "invalid json");

        // Act
        var restored = await _backupManager.RestoreBackupAsync(_testBackupPath);

        // Assert
        Assert.False(restored);
    }

    [Fact]
    public async Task VerifyBackupAsync_WithValidBackup_ReturnsTrue()
    {
        // Arrange
        await CreateTestFiles();
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Act
        var valid = await _backupManager.VerifyBackupAsync(_testBackupPath);

        // Assert
        Assert.True(valid);
    }

    [Fact]
    public async Task VerifyBackupAsync_WithoutMetadata_ReturnsFalse()
    {
        // Arrange
        Directory.CreateDirectory(_testBackupPath);

        // Act
        var valid = await _backupManager.VerifyBackupAsync(_testBackupPath);

        // Assert
        Assert.False(valid);
    }

    [Fact]
    public async Task ListBackupsAsync_ReturnsAllBackups()
    {
        // Arrange
        var backupRoot = Path.Combine(Path.GetTempPath(), $"relay-backups-{Guid.NewGuid()}");
        Directory.CreateDirectory(backupRoot);

        await CreateTestFiles();

        // Create multiple backups
        var backup1 = Path.Combine(backupRoot, "backup_20250101_120000");
        var backup2 = Path.Combine(backupRoot, "backup_20250102_120000");

        await _backupManager.CreateBackupAsync(_testSourcePath, backup1);
        await Task.Delay(100); // Ensure different timestamps
        await _backupManager.CreateBackupAsync(_testSourcePath, backup2);

        // Act
        var backups = await _backupManager.ListBackupsAsync(backupRoot);

        // Assert
        Assert.Equal(2, backups.Count);
        Assert.True(backups[0].CreatedAt >= backups[1].CreatedAt); // Ordered by date descending

        // Cleanup
        Directory.Delete(backupRoot, true);
    }

    [Fact]
    public async Task DeleteBackupAsync_RemovesBackupAndZip()
    {
        // Arrange
        await CreateTestFiles();
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Act
        var deleted = await _backupManager.DeleteBackupAsync(_testBackupPath);

        // Assert
        Assert.True(deleted);
        Assert.False(Directory.Exists(_testBackupPath));
        Assert.False(File.Exists(_testBackupPath + ".zip"));
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_KeepsOnlyRecentBackups()
    {
        // Arrange
        var backupRoot = Path.Combine(Path.GetTempPath(), $"relay-backups-{Guid.NewGuid()}");
        Directory.CreateDirectory(backupRoot);

        await CreateTestFiles();

        // Create 7 backups
        for (int i = 0; i < 7; i++)
        {
            var backupPath = Path.Combine(backupRoot, $"backup_2025010{i}_120000");
            await _backupManager.CreateBackupAsync(_testSourcePath, backupPath);
            await Task.Delay(50);
        }

        // Act
        var deletedCount = await _backupManager.CleanupOldBackupsAsync(backupRoot, keepCount: 3);

        // Assert
        Assert.Equal(4, deletedCount); // 7 - 3 = 4 deleted
        var remaining = await _backupManager.ListBackupsAsync(backupRoot);
        Assert.Equal(3, remaining.Count);

        // Cleanup
        Directory.Delete(backupRoot, true);
    }

    [Fact]
    public async Task VerifyBackupIntegrityAsync_WithValidBackup_ReturnsValidResult()
    {
        // Arrange
        await CreateTestFiles();
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Act
        var result = await _backupManager.VerifyBackupIntegrityAsync(_testBackupPath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Metadata);
        Assert.True(result.FilesFound > 0);
        Assert.True(result.TotalSize > 0);
        Assert.True(result.HasCompressedArchive);
    }

    [Fact]
    public async Task VerifyBackupIntegrityAsync_WithMissingMetadata_ReturnsInvalid()
    {
        // Arrange
        Directory.CreateDirectory(_testBackupPath);

        // Act
        var result = await _backupManager.VerifyBackupIntegrityAsync(_testBackupPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("metadata"));
    }

    [Fact]
    public async Task VerifyBackupIntegrityAsync_WithFileCountMismatch_AddsWarning()
    {
        // Arrange
        await CreateTestFiles();
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Delete a file from backup to cause mismatch
        var files = Directory.GetFiles(_testBackupPath, "*.cs");
        if (files.Length > 0)
        {
            File.Delete(files[0]);
        }

        // Act
        var result = await _backupManager.VerifyBackupIntegrityAsync(_testBackupPath);

        // Assert
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("File count mismatch"));
    }

    [Fact]
    public async Task EstimateBackupSizeAsync_ReturnsAccurateEstimate()
    {
        // Arrange
        await CreateTestFiles();

        // Act
        var estimate = await _backupManager.EstimateBackupSizeAsync(_testSourcePath);

        // Assert
        Assert.True(estimate.FileCount > 0);
        Assert.True(estimate.TotalSize > 0);
        Assert.True(estimate.EstimatedCompressedSize > 0);
        Assert.True(estimate.EstimatedCompressedSize < estimate.TotalSize);
        Assert.Null(estimate.Error);
        Assert.NotNull(estimate.TotalSizeFormatted);
        Assert.NotNull(estimate.CompressedSizeFormatted);
    }

    [Fact]
    public async Task EstimateBackupSizeAsync_WithNonExistentPath_ReturnsZeroFiles()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var estimate = await _backupManager.EstimateBackupSizeAsync(nonExistentPath);

        // Assert
        Assert.Equal(0, estimate.FileCount);
        Assert.Equal(0, estimate.TotalSize);
    }

    [Fact]
    public async Task CreateBackupAsync_ExcludesBinaryFiles()
    {
        // Arrange
        await CreateTestFiles();

        var dllFile = Path.Combine(_testSourcePath, "test.dll");
        var exeFile = Path.Combine(_testSourcePath, "test.exe");
        await File.WriteAllTextAsync(dllFile, "binary content");
        await File.WriteAllTextAsync(exeFile, "exe content");

        // Act
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Assert
        Assert.False(File.Exists(Path.Combine(_testBackupPath, "test.dll")));
        Assert.False(File.Exists(Path.Combine(_testBackupPath, "test.exe")));
        Assert.True(File.Exists(Path.Combine(_testBackupPath, "test.cs"))); // Source files should be backed up
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesCompressedArchive()
    {
        // Arrange
        await CreateTestFiles();

        // Act
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Assert
        var zipPath = _testBackupPath + ".zip";
        Assert.True(File.Exists(zipPath));
        Assert.True(new FileInfo(zipPath).Length > 0);
    }

    [Fact]
    public async Task BackupVerificationResult_PropertiesWorkCorrectly()
    {
        // Arrange
        var result = new BackupVerificationResult
        {
            BackupPath = "/test/path",
            IsValid = true,
            FilesFound = 10,
            TotalSize = 1024,
            HasCompressedArchive = true,
            CompressedSize = 512
        };

        // Act & Assert
        Assert.Equal("/test/path", result.BackupPath);
        Assert.True(result.IsValid);
        Assert.Equal(10, result.FilesFound);
        Assert.Equal(1024, result.TotalSize);
        Assert.True(result.HasCompressedArchive);
        Assert.Equal(512, result.CompressedSize);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void BackupSizeEstimate_FormatsSizeCorrectly()
    {
        // Arrange
        var estimate = new BackupSizeEstimate
        {
            TotalSize = 1024 * 1024 * 2, // 2 MB
            EstimatedCompressedSize = 1024 * 512 // 512 KB
        };

        // Act & Assert
        Assert.Contains("MB", estimate.TotalSizeFormatted);
        Assert.Contains("KB", estimate.CompressedSizeFormatted);
    }

    private async Task CreateTestFiles()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testSourcePath, "test.cs"),
            "public class Test { }");

        await File.WriteAllTextAsync(
            Path.Combine(_testSourcePath, "test.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testSourcePath))
                Directory.Delete(_testSourcePath, true);
            if (Directory.Exists(_testBackupPath))
                Directory.Delete(_testBackupPath, true);
            if (File.Exists(_testBackupPath + ".zip"))
                File.Delete(_testBackupPath + ".zip");
        }
        catch { }
        GC.SuppressFinalize(this);
    }
}
