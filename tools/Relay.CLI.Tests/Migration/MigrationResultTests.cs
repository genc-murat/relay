using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class MigrationResultTests
{
    [Fact]
    public void MigrationResult_HasDefaultValues()
    {
        // Arrange & Act
        var result = new MigrationResult();

        // Assert
        Assert.Equal(MigrationStatus.NotStarted, result.Status);
        Assert.Equal(default, result.StartTime);
        Assert.Equal(default, result.EndTime);
        Assert.Equal(TimeSpan.Zero, result.Duration);
        Assert.Equal(0, result.FilesModified);
        Assert.Equal(0, result.LinesChanged);
        Assert.Equal(0, result.HandlersMigrated);
        Assert.False(result.CreatedBackup);
        Assert.Null(result.BackupPath);
        Assert.NotNull(result.Changes);
        Assert.Empty(result.Changes);
        Assert.NotNull(result.Issues);
        Assert.Empty(result.Issues);
        Assert.NotNull(result.ManualSteps);
        Assert.Empty(result.ManualSteps);
    }

    [Fact]
    public void MigrationResult_CanSetStatus()
    {
        // Arrange
        MigrationResult result = new()
        {
            // Act
            Status = MigrationStatus.Success
        };

        // Assert
        Assert.Equal(MigrationStatus.Success, result.Status);
    }

    [Fact]
    public void MigrationResult_CanSetStartTime()
    {
        // Arrange
        var result = new MigrationResult();
        var startTime = new DateTime(2023, 6, 1, 10, 0, 0);

        // Act
        result.StartTime = startTime;

        // Assert
        Assert.Equal(startTime, result.StartTime);
    }

    [Fact]
    public void MigrationResult_CanSetEndTime()
    {
        // Arrange
        var result = new MigrationResult();
        var endTime = new DateTime(2023, 6, 1, 10, 30, 0);

        // Act
        result.EndTime = endTime;

        // Assert
        Assert.Equal(endTime, result.EndTime);
    }

    [Fact]
    public void MigrationResult_CanSetDuration()
    {
        // Arrange
        var result = new MigrationResult();
        var duration = TimeSpan.FromMinutes(15);

        // Act
        result.Duration = duration;

        // Assert
        Assert.Equal(duration, result.Duration);
    }

    [Fact]
    public void MigrationResult_CanSetFilesModified()
    {
        // Arrange
        MigrationResult result = new()
        {
            // Act
            FilesModified = 25
        };

        // Assert
        Assert.Equal(25, result.FilesModified);
    }

    [Fact]
    public void MigrationResult_CanSetLinesChanged()
    {
        // Arrange
        MigrationResult result = new()
        {
            // Act
            LinesChanged = 150
        };

        // Assert
        Assert.Equal(150, result.LinesChanged);
    }

    [Fact]
    public void MigrationResult_CanSetHandlersMigrated()
    {
        // Arrange
        MigrationResult result = new()
        {
            // Act
            HandlersMigrated = 12
        };

        // Assert
        Assert.Equal(12, result.HandlersMigrated);
    }

    [Fact]
    public void MigrationResult_CanSetCreatedBackup()
    {
        // Arrange
        MigrationResult result = new()
        {
            // Act
            CreatedBackup = true
        };

        // Assert
        Assert.True(result.CreatedBackup);
    }

    [Fact]
    public void MigrationResult_CanSetBackupPath()
    {
        // Arrange
        MigrationResult result = new()
        {
            // Act
            BackupPath = "/backup/migration"
        };

        // Assert
        Assert.Equal("/backup/migration", result.BackupPath);
    }

    [Fact]
    public void MigrationResult_CanAddChanges()
    {
        // Arrange
        var result = new MigrationResult();
        var change = new MigrationChange
        {
            Category = "Code Changes",
            Type = ChangeType.Add,
            Description = "Added Relay handler",
            FilePath = "/src/Handler.cs"
        };

        // Act
        result.Changes.Add(change);

        // Assert
        Assert.Single(result.Changes);
        Assert.Equal("Code Changes", result.Changes[0].Category);
        Assert.Equal(ChangeType.Add, result.Changes[0].Type);
    }

    [Fact]
    public void MigrationResult_CanAddIssues()
    {
        // Arrange
        var result = new MigrationResult();

        // Act
        result.Issues.Add("Handler migration failed");

        // Assert
        Assert.Single(result.Issues);
        Assert.Equal("Handler migration failed", result.Issues[0]);
    }

    [Fact]
    public void MigrationResult_CanAddManualSteps()
    {
        // Arrange
        var result = new MigrationResult();

        // Act
        result.ManualSteps.Add("Review pipeline behaviors");

        // Assert
        Assert.Single(result.ManualSteps);
        Assert.Equal("Review pipeline behaviors", result.ManualSteps[0]);
    }

    [Fact]
    public void MigrationResult_SupportsObjectInitializer()
    {
        // Arrange & Act
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            StartTime = new DateTime(2023, 6, 1, 9, 0, 0),
            EndTime = new DateTime(2023, 6, 1, 9, 45, 0),
            Duration = TimeSpan.FromMinutes(45),
            FilesModified = 20,
            LinesChanged = 300,
            HandlersMigrated = 15,
            CreatedBackup = true,
            BackupPath = "/backups/migration-123"
        };

        // Assert
        Assert.Equal(MigrationStatus.Success, result.Status);
        Assert.Equal(new DateTime(2023, 6, 1, 9, 0, 0), result.StartTime);
        Assert.Equal(new DateTime(2023, 6, 1, 9, 45, 0), result.EndTime);
        Assert.Equal(TimeSpan.FromMinutes(45), result.Duration);
        Assert.Equal(20, result.FilesModified);
        Assert.Equal(300, result.LinesChanged);
        Assert.Equal(15, result.HandlersMigrated);
        Assert.True(result.CreatedBackup);
        Assert.Equal("/backups/migration-123", result.BackupPath);
    }

    [Fact]
    public void MigrationResult_CanCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            FilesModified = 10,
            HandlersMigrated = 8,
            CreatedBackup = true
        };

        // Assert
        Assert.Equal(MigrationStatus.Success, result.Status);
        Assert.Equal(10, result.FilesModified);
        Assert.Equal(8, result.HandlersMigrated);
        Assert.True(result.CreatedBackup);
    }

    [Fact]
    public void MigrationResult_CanCreateFailedResult()
    {
        // Arrange & Act
        var result = new MigrationResult
        {
            Status = MigrationStatus.Failed,
            FilesModified = 2,
            HandlersMigrated = 1,
            CreatedBackup = false
        };
        result.Issues.Add("Syntax error in Handler.cs");
        result.ManualSteps.Add("Fix syntax error manually");

        // Assert
        Assert.Equal(MigrationStatus.Failed, result.Status);
        Assert.Equal(2, result.FilesModified);
        Assert.Equal(1, result.HandlersMigrated);
        Assert.False(result.CreatedBackup);
        Assert.Single(result.Issues);
        Assert.Single(result.ManualSteps);
    }
}
