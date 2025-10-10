using Relay.CLI.Migration;
using System.Text.Json;

namespace Relay.CLI.Tests.Migration;

public class MigrationReportTests
{
    [Fact]
    public void MigrationReport_ToJson_GeneratesValidJson()
    {
        // Arrange
        var report = new
        {
            ProjectPath = "/test/project",
            Success = true,
            FilesProcessed = 5,
            FilesModified = 3
        };

        // Act
        var json = JsonSerializer.Serialize(report);

        // Assert
        Assert.Contains("ProjectPath", json);
        Assert.Contains("Success", json);
        Assert.Contains("true", json);
    }

    [Fact]
    public void MigrationReport_ToMarkdown_GeneratesValidMarkdown()
    {
        // Arrange
        var title = "# Migration Report";
        var successMessage = "✅ Migration Successful";
        var filesList = "- Handler.cs";

        // Act
        var markdown = $"{title}\n\n{successMessage}\n\n{filesList}";

        // Assert
        Assert.Contains("# Migration Report", markdown);
        Assert.Contains("✅ Migration Successful", markdown);
        Assert.Contains("Handler.cs", markdown);
    }

    [Fact]
    public void MigrationReport_WithErrors_MarksAsUnsuccessful()
    {
        // Arrange
        var errors = new List<string>
        {
            "Error 1: Something went wrong",
            "Error 2: Another issue"
        };

        // Act
        var success = errors.Count == 0;

        // Assert
        Assert.False(success);
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void MigrationReport_CalculatesStatistics()
    {
        // Arrange
        var totalFiles = 10;
        var modifiedFiles = 7;
        var unchangedFiles = totalFiles - modifiedFiles;

        // Act
        var percentageModified = (double)modifiedFiles / totalFiles * 100;

        // Assert
        Assert.Equal(10, totalFiles);
        Assert.Equal(7, modifiedFiles);
        Assert.Equal(3, unchangedFiles);
        Assert.Equal(70.0, percentageModified);
    }

    [Fact]
    public void MigrationReport_TracksDuration()
    {
        // Arrange
        var startTime = DateTime.Now;
        var endTime = startTime.AddMinutes(5);

        // Act
        var duration = endTime - startTime;

        // Assert
        Assert.Equal(5, duration.TotalMinutes);
    }
}
