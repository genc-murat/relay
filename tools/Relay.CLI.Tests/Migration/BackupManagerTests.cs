using Relay.CLI.Migration;

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
        Directory.Exists(backupPath).Should().BeTrue();
        backupPath.Should().Be(_testBackupPath);
    }

    [Fact]
    public async Task CreateBackupAsync_CopiesAllFiles()
    {
        // Arrange
        await CreateTestFiles();

        // Act
        await _backupManager.CreateBackupAsync(_testSourcePath, _testBackupPath);

        // Assert
        File.Exists(Path.Combine(_testBackupPath, "test.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_testBackupPath, "test.csproj")).Should().BeTrue();
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
        File.Exists(metadataPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(metadataPath);
        content.Should().Contain("BackupId");
        content.Should().Contain("SourcePath");
        content.Should().Contain("FileCount");
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
        Directory.Exists(Path.Combine(_testBackupPath, "bin")).Should().BeFalse();
        Directory.Exists(Path.Combine(_testBackupPath, "obj")).Should().BeFalse();
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
        restored.Should().BeTrue();
        File.Exists(Path.Combine(_testSourcePath, "test.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_testSourcePath, "test.csproj")).Should().BeTrue();
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
        valid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyBackupAsync_WithoutMetadata_ReturnsFalse()
    {
        // Arrange
        Directory.CreateDirectory(_testBackupPath);

        // Act
        var valid = await _backupManager.VerifyBackupAsync(_testBackupPath);

        // Assert
        valid.Should().BeFalse();
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
    }
}
