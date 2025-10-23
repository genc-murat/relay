using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class BackupMetadataTests
{
    [Fact]
    public void BackupMetadata_HasDefaultValues()
    {
        // Arrange & Act
        var metadata = new BackupMetadata();

        // Assert
        Assert.Equal("", metadata.BackupId);
        Assert.Equal("", metadata.SourcePath);
        Assert.Equal("", metadata.BackupPath);
        Assert.Equal(default, metadata.CreatedAt);
        Assert.Equal("", metadata.ToolVersion);
        Assert.Equal(0, metadata.FileCount);
        Assert.Equal(0L, metadata.TotalSize);
    }

    [Fact]
    public void BackupMetadata_CanSetBackupId()
    {
        // Arrange
        BackupMetadata metadata = new()
        {
            // Act
            BackupId = "backup-123"
        };

        // Assert
        Assert.Equal("backup-123", metadata.BackupId);
    }

    [Fact]
    public void BackupMetadata_CanSetSourcePath()
    {
        // Arrange
        BackupMetadata metadata = new()
        {
            // Act
            SourcePath = "/source/path"
        };

        // Assert
        Assert.Equal("/source/path", metadata.SourcePath);
    }

    [Fact]
    public void BackupMetadata_CanSetBackupPath()
    {
        // Arrange
        BackupMetadata metadata = new()
        {
            // Act
            BackupPath = "/backup/path"
        };

        // Assert
        Assert.Equal("/backup/path", metadata.BackupPath);
    }

    [Fact]
    public void BackupMetadata_CanSetCreatedAt()
    {
        // Arrange
        var metadata = new BackupMetadata();
        var testDate = new DateTime(2023, 5, 10, 14, 30, 0);

        // Act
        metadata.CreatedAt = testDate;

        // Assert
        Assert.Equal(testDate, metadata.CreatedAt);
    }

    [Fact]
    public void BackupMetadata_CanSetToolVersion()
    {
        // Arrange
        BackupMetadata metadata = new()
        {
            // Act
            ToolVersion = "1.2.3"
        };

        // Assert
        Assert.Equal("1.2.3", metadata.ToolVersion);
    }

    [Fact]
    public void BackupMetadata_CanSetFileCount()
    {
        // Arrange
        BackupMetadata metadata = new()
        {
            // Act
            FileCount = 150
        };

        // Assert
        Assert.Equal(150, metadata.FileCount);
    }

    [Fact]
    public void BackupMetadata_CanSetTotalSize()
    {
        // Arrange
        BackupMetadata metadata = new()
        {
            // Act
            TotalSize = 1024000L
        };

        // Assert
        Assert.Equal(1024000L, metadata.TotalSize);
    }

    [Fact]
    public void BackupMetadata_SupportsObjectInitializer()
    {
        // Arrange & Act
        var metadata = new BackupMetadata
        {
            BackupId = "test-backup-456",
            SourcePath = "/original/source",
            BackupPath = "/backup/destination",
            CreatedAt = new DateTime(2023, 7, 20, 9, 15, 0),
            ToolVersion = "2.0.0",
            FileCount = 75,
            TotalSize = 512000L
        };

        // Assert
        Assert.Equal("test-backup-456", metadata.BackupId);
        Assert.Equal("/original/source", metadata.SourcePath);
        Assert.Equal("/backup/destination", metadata.BackupPath);
        Assert.Equal(new DateTime(2023, 7, 20, 9, 15, 0), metadata.CreatedAt);
        Assert.Equal("2.0.0", metadata.ToolVersion);
        Assert.Equal(75, metadata.FileCount);
        Assert.Equal(512000L, metadata.TotalSize);
    }
}
