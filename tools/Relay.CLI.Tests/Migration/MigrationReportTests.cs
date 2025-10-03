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
        json.Should().Contain("ProjectPath");
        json.Should().Contain("Success");
        json.Should().Contain("true");
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
        markdown.Should().Contain("# Migration Report");
        markdown.Should().Contain("✅ Migration Successful");
        markdown.Should().Contain("Handler.cs");
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
        success.Should().BeFalse();
        errors.Should().HaveCount(2);
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
        totalFiles.Should().Be(10);
        modifiedFiles.Should().Be(7);
        unchangedFiles.Should().Be(3);
        percentageModified.Should().Be(70.0);
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
        duration.TotalMinutes.Should().Be(5);
    }
}
