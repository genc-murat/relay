using Relay.CLI.Commands;
using System.Text.Json;

namespace Relay.CLI.Tests.Commands;

public class AIPerformanceIssueTests
{
    [Fact]
    public void AIPerformanceIssue_ShouldHaveSeverityProperty()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue { Severity = "High" };

        // Assert
        issue.Severity.Should().Be("High");
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHaveDescriptionProperty()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue { Description = "Memory leak detected" };

        // Assert
        issue.Description.Should().Be("Memory leak detected");
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHaveLocationProperty()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue { Location = "Service.cs:45" };

        // Assert
        issue.Location.Should().Be("Service.cs:45");
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHaveImpactProperty()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue { Impact = "Critical" };

        // Assert
        issue.Impact.Should().Be("Critical");
    }

    [Fact]
    public void AIPerformanceIssue_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue();

        // Assert
        issue.Severity.Should().Be("");
        issue.Description.Should().Be("");
        issue.Location.Should().Be("");
        issue.Impact.Should().Be("");
    }

    [Fact]
    public void AIPerformanceIssue_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue
        {
            Severity = "Critical",
            Description = "Database connection pool exhaustion",
            Location = "DatabaseService.cs:127",
            Impact = "High"
        };

        // Assert
        issue.Severity.Should().Be("Critical");
        issue.Description.Should().Be("Database connection pool exhaustion");
        issue.Location.Should().Be("DatabaseService.cs:127");
        issue.Impact.Should().Be("High");
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHandleDifferentSeverityLevels()
    {
        // Arrange
        var severities = new[] { "Low", "Medium", "High", "Critical" };

        foreach (var severity in severities)
        {
            // Act
            var issue = new AIPerformanceIssue { Severity = severity };

            // Assert
            issue.Severity.Should().Be(severity);
        }
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHandleDifferentImpactLevels()
    {
        // Arrange
        var impacts = new[] { "Low", "Medium", "High", "Critical" };

        foreach (var impact in impacts)
        {
            // Act
            var issue = new AIPerformanceIssue { Impact = impact };

            // Assert
            issue.Impact.Should().Be(impact);
        }
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHandleComplexLocationStrings()
    {
        // Arrange
        var locations = new[]
        {
            "C:\\Projects\\MyApp\\Services\\UserService.cs:156",
            "/src/controllers/AuthController.cs:89",
            "Handler.cs",
            "Namespace.Class.Method()"
        };

        foreach (var location in locations)
        {
            // Act
            var issue = new AIPerformanceIssue { Location = location };

            // Assert
            issue.Location.Should().Be(location);
        }
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHandleLongDescriptions()
    {
        // Arrange
        var longDescription = "This is a very long description that explains in detail the performance issue that was detected by the AI analysis system. It includes multiple sentences and provides comprehensive information about the problem, its causes, and potential implications for the system's performance.";

        // Act
        var issue = new AIPerformanceIssue { Description = longDescription };

        // Assert
        issue.Description.Should().Be(longDescription);
    }

    [Fact]
    public void AIPerformanceIssue_ShouldSerializeToJson()
    {
        // Arrange
        var issue = new AIPerformanceIssue
        {
            Severity = "High",
            Description = "Inefficient database query",
            Location = "Repository.cs:78",
            Impact = "Medium"
        };

        // Act
        var json = JsonSerializer.Serialize(issue);
        var deserialized = JsonSerializer.Deserialize<AIPerformanceIssue>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Severity.Should().Be("High");
        deserialized.Description.Should().Be("Inefficient database query");
        deserialized.Location.Should().Be("Repository.cs:78");
        deserialized.Impact.Should().Be("Medium");
    }

    [Fact]
    public void AIPerformanceIssue_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""Severity"": ""Critical"",
            ""Description"": ""Memory leak in background service"",
            ""Location"": ""BackgroundWorker.cs:234"",
            ""Impact"": ""High""
        }";

        // Act
        var issue = JsonSerializer.Deserialize<AIPerformanceIssue>(json);

        // Assert
        issue.Should().NotBeNull();
        issue!.Severity.Should().Be("Critical");
        issue.Description.Should().Be("Memory leak in background service");
        issue.Location.Should().Be("BackgroundWorker.cs:234");
        issue.Impact.Should().Be("High");
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHandleEmptyStrings()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue
        {
            Severity = "",
            Description = "",
            Location = "",
            Impact = ""
        };

        // Assert
        issue.Severity.Should().Be("");
        issue.Description.Should().Be("");
        issue.Location.Should().Be("");
        issue.Impact.Should().Be("");
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHandleNullStringValuesGracefully()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue();

        // Assert - Default initialization should provide empty strings
        issue.Severity.Should().NotBeNull();
        issue.Description.Should().NotBeNull();
        issue.Location.Should().NotBeNull();
        issue.Impact.Should().NotBeNull();
        issue.Severity.Should().Be("");
        issue.Description.Should().Be("");
        issue.Location.Should().Be("");
        issue.Impact.Should().Be("");
    }
}