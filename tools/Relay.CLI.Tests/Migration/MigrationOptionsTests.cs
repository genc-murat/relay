using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class MigrationOptionsTests
{
    [Fact]
    public void MigrationOptions_HasDefaultValues()
    {
        // Arrange & Act
        var options = new MigrationOptions();

        // Assert
        Assert.Equal("MediatR", options.SourceFramework);
        Assert.Equal("Relay", options.TargetFramework);
        Assert.Equal("", options.ProjectPath);
        Assert.False(options.AnalyzeOnly);
        Assert.False(options.DryRun);
        Assert.False(options.ShowPreview);
        Assert.True(options.CreateBackup);
        Assert.Equal(".backup", options.BackupPath);
        Assert.False(options.Interactive);
        Assert.False(options.Aggressive);
        Assert.True(options.EnableParallelProcessing);
        Assert.False(options.UseSideBySideDiff);
    }

    [Fact]
    public void MigrationOptions_CanSetSourceFramework()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.SourceFramework = "CustomFramework";

        // Assert
        Assert.Equal("CustomFramework", options.SourceFramework);
    }

    [Fact]
    public void MigrationOptions_CanSetTargetFramework()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.TargetFramework = "CustomTarget";

        // Assert
        Assert.Equal("CustomTarget", options.TargetFramework);
    }

    [Fact]
    public void MigrationOptions_CanSetProjectPath()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.ProjectPath = "/path/to/project";

        // Assert
        Assert.Equal("/path/to/project", options.ProjectPath);
    }

    [Fact]
    public void MigrationOptions_CanSetAnalyzeOnly()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.AnalyzeOnly = true;

        // Assert
        Assert.True(options.AnalyzeOnly);
    }

    [Fact]
    public void MigrationOptions_CanSetDryRun()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.DryRun = true;

        // Assert
        Assert.True(options.DryRun);
    }

    [Fact]
    public void MigrationOptions_CanSetShowPreview()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.ShowPreview = true;

        // Assert
        Assert.True(options.ShowPreview);
    }

    [Fact]
    public void MigrationOptions_CanSetCreateBackup()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.CreateBackup = false;

        // Assert
        Assert.False(options.CreateBackup);
    }

    [Fact]
    public void MigrationOptions_CanSetBackupPath()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.BackupPath = "/custom/backup/path";

        // Assert
        Assert.Equal("/custom/backup/path", options.BackupPath);
    }

    [Fact]
    public void MigrationOptions_CanSetInteractive()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.Interactive = true;

        // Assert
        Assert.True(options.Interactive);
    }

    [Fact]
    public void MigrationOptions_CanSetAggressive()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.Aggressive = true;

        // Assert
        Assert.True(options.Aggressive);
    }

    [Fact]
    public void MigrationOptions_CanSetUseSideBySideDiff()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.UseSideBySideDiff = true;

        // Assert
        Assert.True(options.UseSideBySideDiff);
    }

    [Fact]
    public void MigrationOptions_CanSetEnableParallelProcessing()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.EnableParallelProcessing = false;

        // Assert
        Assert.False(options.EnableParallelProcessing);
    }

    [Fact]
    public void MigrationOptions_CanSetMaxDegreeOfParallelism()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.MaxDegreeOfParallelism = 4;

        // Assert
        Assert.Equal(4, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void MigrationOptions_CanSetParallelBatchSize()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.ParallelBatchSize = 20;

        // Assert
        Assert.Equal(20, options.ParallelBatchSize);
    }

    [Fact]
    public void MigrationOptions_CanSetProgressReportInterval()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.ProgressReportInterval = 1000;

        // Assert
        Assert.Equal(1000, options.ProgressReportInterval);
    }

    [Fact]
    public void MigrationOptions_CanSetContinueOnError()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act
        options.ContinueOnError = false;

        // Assert
        Assert.False(options.ContinueOnError);
    }

    [Fact]
    public void MigrationOptions_OnProgressCanBeSet()
    {
        // Arrange
        var options = new MigrationOptions();
        var called = false;
        Action<MigrationProgress> callback = _ => called = true;

        // Act
        options.OnProgress = callback;
        options.OnProgress?.Invoke(new MigrationProgress());

        // Assert
        Assert.NotNull(options.OnProgress);
        Assert.True(called);
    }

    [Fact]
    public void MigrationOptions_DefaultMaxDegreeOfParallelismIsProcessorCount()
    {
        // Arrange & Act
        var options = new MigrationOptions();

        // Assert
        Assert.Equal(Environment.ProcessorCount, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void MigrationOptions_DefaultParallelBatchSizeIs10()
    {
        // Arrange & Act
        var options = new MigrationOptions();

        // Assert
        Assert.Equal(10, options.ParallelBatchSize);
    }

    [Fact]
    public void MigrationOptions_DefaultProgressReportIntervalIs500()
    {
        // Arrange & Act
        var options = new MigrationOptions();

        // Assert
        Assert.Equal(500, options.ProgressReportInterval);
    }

    [Fact]
    public void MigrationOptions_DefaultContinueOnErrorIsTrue()
    {
        // Arrange & Act
        var options = new MigrationOptions();

        // Assert
        Assert.True(options.ContinueOnError);
    }

    [Fact]
    public void MigrationOptions_SupportsObjectInitializer()
    {
        // Arrange & Act
        var options = new MigrationOptions
        {
            SourceFramework = "MediatR",
            TargetFramework = "Relay",
            ProjectPath = "/test/path",
            AnalyzeOnly = true,
            DryRun = true,
            ShowPreview = true,
            CreateBackup = false,
            BackupPath = "/backup",
            Interactive = true,
            Aggressive = true,
            UseSideBySideDiff = true,
            EnableParallelProcessing = false,
            MaxDegreeOfParallelism = 2,
            ParallelBatchSize = 5,
            ProgressReportInterval = 250,
            ContinueOnError = false
        };

        // Assert
        Assert.Equal("MediatR", options.SourceFramework);
        Assert.Equal("Relay", options.TargetFramework);
        Assert.Equal("/test/path", options.ProjectPath);
        Assert.True(options.AnalyzeOnly);
        Assert.True(options.DryRun);
        Assert.True(options.ShowPreview);
        Assert.False(options.CreateBackup);
        Assert.Equal("/backup", options.BackupPath);
        Assert.True(options.Interactive);
        Assert.True(options.Aggressive);
        Assert.True(options.UseSideBySideDiff);
        Assert.False(options.EnableParallelProcessing);
        Assert.Equal(2, options.MaxDegreeOfParallelism);
        Assert.Equal(5, options.ParallelBatchSize);
        Assert.Equal(250, options.ProgressReportInterval);
        Assert.False(options.ContinueOnError);
    }
}
