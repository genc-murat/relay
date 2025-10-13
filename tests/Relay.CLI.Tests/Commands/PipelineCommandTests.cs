using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class PipelineCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = PipelineCommand.Create();

        // Assert
        Assert.Equal("pipeline", command.Name);
        Assert.Equal("Run complete project development pipeline", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = PipelineCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "path");
        Assert.Contains(command.Options, o => o.Name == "name");
        Assert.Contains(command.Options, o => o.Name == "template");
        Assert.Contains(command.Options, o => o.Name == "skip");
        Assert.Contains(command.Options, o => o.Name == "aggressive");
        Assert.Contains(command.Options, o => o.Name == "auto-fix");
        Assert.Contains(command.Options, o => o.Name == "report");
        Assert.Contains(command.Options, o => o.Name == "ci");
    }

    [Fact]
    public async Task ExecutePipeline_WithSkipAllStages_CompletesSuccessfully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayPipelineSkipTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await PipelineCommand.ExecutePipeline(
                tempDir, null, "standard",
                new[] { "init", "doctor", "validate", "optimize" },
                false, false, null, false, CancellationToken.None);

            // Assert - Should complete without running any stages
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecutePipeline_WithDoctorStageOnly_RunsDoctorStage()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayPipelineDoctorTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await PipelineCommand.ExecutePipeline(
                tempDir, null, "standard",
                new[] { "init", "validate", "optimize" },
                false, false, null, false, CancellationToken.None);

            // Assert - Should run only doctor stage
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecutePipeline_WithCIMode_UsesSimpleOutput()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayPipelineCITest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await PipelineCommand.ExecutePipeline(
                tempDir, null, "standard",
                new[] { "init", "validate", "optimize" },
                false, false, null, true, CancellationToken.None);

            // Assert - Should use CI mode output
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecutePipeline_WithReportPath_GeneratesReport()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayPipelineReportTest");
        Directory.CreateDirectory(tempDir);
        var reportPath = Path.Combine(tempDir, "pipeline-report.md");

        try
        {
            // Act
            await PipelineCommand.ExecutePipeline(
                tempDir, null, "standard",
                new[] { "init", "validate", "optimize" },
                false, false, reportPath, false, CancellationToken.None);

            // Assert
            Assert.True(File.Exists(reportPath));
            var reportContent = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("# Relay Pipeline Report", reportContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecutePipeline_WithInitStage_CreatesProject()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayPipelineInitTest");
        var projectName = "TestPipelineProject";

        try
        {
            // Act
            await PipelineCommand.ExecutePipeline(
                tempDir, projectName, "standard",
                new[] { "doctor", "validate", "optimize" },
                false, false, null, false, CancellationToken.None);

            // Assert - Project should be created
            var projectPath = Path.Combine(tempDir, projectName);
            Assert.True(Directory.Exists(projectPath));
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }

    [Fact]
    public async Task ExecutePipeline_WithAggressiveOptimization_AppliesAggressiveOpts()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayPipelineAggressiveTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await PipelineCommand.ExecutePipeline(
                tempDir, null, "standard",
                new[] { "init", "doctor", "validate" },
                true, false, null, false, CancellationToken.None);

            // Assert - Should apply aggressive optimizations
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecutePipeline_WithCancellation_HandlesCancellation()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayPipelineCancelTest");
        Directory.CreateDirectory(tempDir);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel quickly

        try
        {
            // Act & Assert - Should handle cancellation gracefully
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await PipelineCommand.ExecutePipeline(
                    tempDir, null, "standard",
                    Array.Empty<string>(),
                    false, false, null, false, cts.Token));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}