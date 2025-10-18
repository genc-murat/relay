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
        Assert.Equal("High", issue.Severity);
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHaveDescriptionProperty()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue { Description = "Memory leak detected" };

        // Assert
        Assert.Equal("Memory leak detected", issue.Description);
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHaveLocationProperty()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue { Location = "Service.cs:45" };

        // Assert
        Assert.Equal("Service.cs:45", issue.Location);
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHaveImpactProperty()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue { Impact = "Critical" };

        // Assert
        Assert.Equal("Critical", issue.Impact);
    }

    [Fact]
    public void AIPerformanceIssue_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue();

        // Assert
        Assert.Equal("", issue.Severity);
        Assert.Equal("", issue.Description);
        Assert.Equal("", issue.Location);
        Assert.Equal("", issue.Impact);
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
        Assert.Equal("Critical", issue.Severity);
        Assert.Equal("Database connection pool exhaustion", issue.Description);
        Assert.Equal("DatabaseService.cs:127", issue.Location);
        Assert.Equal("High", issue.Impact);
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
            Assert.Equal(severity, issue.Severity);
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
            Assert.Equal(impact, issue.Impact);
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
            Assert.Equal(location, issue.Location);
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
        Assert.Equal(longDescription, issue.Description);
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
        Assert.NotNull(deserialized);
        Assert.Equal("High", deserialized.Severity);
        Assert.Equal("Inefficient database query", deserialized.Description);
        Assert.Equal("Repository.cs:78", deserialized.Location);
        Assert.Equal("Medium", deserialized.Impact);
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
        Assert.NotNull(issue);
        Assert.Equal("Critical", issue.Severity);
        Assert.Equal("Memory leak in background service", issue.Description);
        Assert.Equal("BackgroundWorker.cs:234", issue.Location);
        Assert.Equal("High", issue.Impact);
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
        Assert.Equal("", issue.Severity);
        Assert.Equal("", issue.Description);
        Assert.Equal("", issue.Location);
        Assert.Equal("", issue.Impact);
    }

    [Fact]
    public void AIPerformanceIssue_ShouldHandleNullStringValuesGracefully()
    {
        // Arrange & Act
        var issue = new AIPerformanceIssue();

        // Assert - Default initialization should provide empty strings
        Assert.NotNull(issue.Severity);
        Assert.NotNull(issue.Description);
        Assert.NotNull(issue.Location);
        Assert.NotNull(issue.Impact);
        Assert.Equal("", issue.Severity);
        Assert.Equal("", issue.Description);
        Assert.Equal("", issue.Location);
        Assert.Equal("", issue.Impact);
    }
}
