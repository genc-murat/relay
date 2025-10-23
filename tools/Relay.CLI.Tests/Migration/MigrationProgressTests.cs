using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class MigrationProgressTests
{
    [Fact]
    public void MigrationProgress_HasDefaultValues()
    {
        // Arrange & Act
        var progress = new MigrationProgress();

        // Assert
        Assert.Equal(MigrationStage.Initializing, progress.Stage);
        Assert.Null(progress.CurrentFile);
        Assert.Equal(0, progress.TotalFiles);
        Assert.Equal(0, progress.ProcessedFiles);
        Assert.Equal(0.0, progress.PercentComplete);
        Assert.Equal("", progress.Message);
        Assert.Null(progress.EstimatedTimeRemaining);
        Assert.Equal(TimeSpan.Zero, progress.ElapsedTime);
        Assert.Equal(0, progress.FilesModified);
        Assert.Equal(0, progress.HandlersMigrated);
        Assert.False(progress.IsParallel);
    }

    [Fact]
    public void MigrationProgress_CanSetStage()
    {
        // Arrange
        MigrationProgress progress = new()
        {
            // Act
            Stage = MigrationStage.TransformingCode
        };

        // Assert
        Assert.Equal(MigrationStage.TransformingCode, progress.Stage);
    }

    [Fact]
    public void MigrationProgress_CanSetCurrentFile()
    {
        // Arrange
        MigrationProgress progress = new()
        {
            // Act
            CurrentFile = "/src/Handler.cs"
        };

        // Assert
        Assert.Equal("/src/Handler.cs", progress.CurrentFile);
    }

    [Fact]
    public void MigrationProgress_CanSetTotalFiles()
    {
        // Arrange
        MigrationProgress progress = new()
        {
            // Act
            TotalFiles = 100
        };

        // Assert
        Assert.Equal(100, progress.TotalFiles);
    }

    [Fact]
    public void MigrationProgress_CanSetProcessedFiles()
    {
        // Arrange
        MigrationProgress progress = new()
        {
            // Act
            ProcessedFiles = 25
        };

        // Assert
        Assert.Equal(25, progress.ProcessedFiles);
    }

    [Fact]
    public void MigrationProgress_CanSetMessage()
    {
        // Arrange
        MigrationProgress progress = new()
        {
            // Act
            Message = "Processing handlers"
        };

        // Assert
        Assert.Equal("Processing handlers", progress.Message);
    }

    [Fact]
    public void MigrationProgress_CanSetEstimatedTimeRemaining()
    {
        // Arrange
        var progress = new MigrationProgress();
        var timeRemaining = TimeSpan.FromMinutes(5);

        // Act
        progress.EstimatedTimeRemaining = timeRemaining;

        // Assert
        Assert.Equal(timeRemaining, progress.EstimatedTimeRemaining);
    }

    [Fact]
    public void MigrationProgress_CanSetElapsedTime()
    {
        // Arrange
        var progress = new MigrationProgress();
        var elapsed = TimeSpan.FromSeconds(30);

        // Act
        progress.ElapsedTime = elapsed;

        // Assert
        Assert.Equal(elapsed, progress.ElapsedTime);
    }

    [Fact]
    public void MigrationProgress_CanSetFilesModified()
    {
        // Arrange
        MigrationProgress progress = new()
        {
            // Act
            FilesModified = 15
        };

        // Assert
        Assert.Equal(15, progress.FilesModified);
    }

    [Fact]
    public void MigrationProgress_CanSetHandlersMigrated()
    {
        // Arrange
        MigrationProgress progress = new()
        {
            // Act
            HandlersMigrated = 8
        };

        // Assert
        Assert.Equal(8, progress.HandlersMigrated);
    }

    [Fact]
    public void MigrationProgress_CanSetIsParallel()
    {
        // Arrange
        MigrationProgress progress = new()
        {
            // Act
            IsParallel = true
        };

        // Assert
        Assert.True(progress.IsParallel);
    }

    [Fact]
    public void MigrationProgress_PercentComplete_ReturnsZeroWhenTotalFilesIsZero()
    {
        // Arrange
        var progress = new MigrationProgress
        {
            TotalFiles = 0,
            ProcessedFiles = 0
        };

        // Act & Assert
        Assert.Equal(0.0, progress.PercentComplete);
    }

    [Fact]
    public void MigrationProgress_PercentComplete_CalculatesCorrectly()
    {
        // Arrange
        var progress = new MigrationProgress
        {
            TotalFiles = 10,
            ProcessedFiles = 3
        };

        // Act & Assert
        Assert.Equal(30.0, progress.PercentComplete);
    }

    [Fact]
    public void MigrationProgress_PercentComplete_Returns100WhenAllFilesProcessed()
    {
        // Arrange
        var progress = new MigrationProgress
        {
            TotalFiles = 5,
            ProcessedFiles = 5
        };

        // Act & Assert
        Assert.Equal(100.0, progress.PercentComplete);
    }

    [Fact]
    public void MigrationProgress_PercentComplete_HandlesPartialProgress()
    {
        // Arrange
        var progress = new MigrationProgress
        {
            TotalFiles = 7,
            ProcessedFiles = 2
        };

        // Act & Assert
        Assert.Equal(28.571428571428573, progress.PercentComplete, 10); // Allow for floating point precision
    }

    [Fact]
    public void MigrationProgress_SupportsObjectInitializer()
    {
        // Arrange & Act
        var progress = new MigrationProgress
        {
            Stage = MigrationStage.Analyzing,
            CurrentFile = "/src/Program.cs",
            TotalFiles = 50,
            ProcessedFiles = 10,
            Message = "Analyzing project structure",
            EstimatedTimeRemaining = TimeSpan.FromMinutes(3),
            ElapsedTime = TimeSpan.FromSeconds(45),
            FilesModified = 5,
            HandlersMigrated = 3,
            IsParallel = true
        };

        // Assert
        Assert.Equal(MigrationStage.Analyzing, progress.Stage);
        Assert.Equal("/src/Program.cs", progress.CurrentFile);
        Assert.Equal(50, progress.TotalFiles);
        Assert.Equal(10, progress.ProcessedFiles);
        Assert.Equal(20.0, progress.PercentComplete);
        Assert.Equal("Analyzing project structure", progress.Message);
        Assert.Equal(TimeSpan.FromMinutes(3), progress.EstimatedTimeRemaining);
        Assert.Equal(TimeSpan.FromSeconds(45), progress.ElapsedTime);
        Assert.Equal(5, progress.FilesModified);
        Assert.Equal(3, progress.HandlersMigrated);
        Assert.True(progress.IsParallel);
    }
}
