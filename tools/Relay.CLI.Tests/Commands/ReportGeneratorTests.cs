using Relay.CLI.Commands;
using Relay.CLI.Commands.Models;
using Relay.CLI.Commands.Models.Performance;

namespace Relay.CLI.Tests.Commands;

public class ReportGeneratorTests
{
    [Fact]
    public void GenerateHtmlAnalysisReport_ShouldGenerateValidHtml()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 0),
            ProjectFiles = ["Project.csproj", "README.md"],
            SourceFiles = ["Program.cs", "Startup.cs"],
            Handlers = [new HandlerInfo { Name = "TestHandler" }],
            Requests = [new RequestInfo { Name = "TestRequest" }],
            PerformanceIssues = [
                new PerformanceIssue { Description = "Memory leak", Severity = "High", Recommendation = "Fix memory leak", PotentialImprovement = "Reduce memory usage" }
            ],
            Recommendations = [
                new Recommendation { Title = "Add caching", Description = "Implement caching", Actions = ["Add cache layer"], EstimatedImpact = "Improve performance" }
            ]
        };

        // Act
        var html = ReportGenerator.GenerateHtmlAnalysisReport(analysis);

        // Assert
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html>", html);
        Assert.Contains("</html>", html);
        Assert.Contains("Relay Project Analysis Report", html);
        Assert.Contains("2023-10-15 14:30:00", html);
        Assert.Contains("Project Files: 2", html);
        Assert.Contains("Source Files: 2", html);
        Assert.Contains("Handlers Found: 1", html);
        Assert.Contains("Requests Found: 1", html);
        Assert.Contains("Memory leak", html);
        Assert.Contains("Add caching", html);
    }

    [Fact]
    public void GenerateHtmlAnalysisReport_ShouldIncludeOverallScore()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 0),
            HasRelayCore = true,
            HasLogging = true,
            PerformanceIssues = [
                new PerformanceIssue { Severity = "High" }
            ]
        };

        // Act
        var html = ReportGenerator.GenerateHtmlAnalysisReport(analysis);

        // Assert
        Assert.Contains("Overall Score:", html);
        // Score should be calculated: 10 - 2 (high issue) + 0.5 (relay) + 0.3 (logging) = 8.8
        Assert.Contains("8.8/10", html);
    }

    [Fact]
    public void GenerateHtmlAnalysisReport_ShouldHandleEmptyCollections()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 0),
            ProjectFiles = [],
            SourceFiles = [],
            Handlers = [],
            Requests = [],
            PerformanceIssues = [],
            Recommendations = []
        };

        // Act
        var html = ReportGenerator.GenerateHtmlAnalysisReport(analysis);

        // Assert
        Assert.Contains("Project Files: 0", html);
        Assert.Contains("Source Files: 0", html);
        Assert.Contains("Handlers Found: 0", html);
        Assert.Contains("Requests Found: 0", html);
        // HTML always includes section headers even with empty collections
        Assert.Contains("⚡ Performance Issues", html);
        Assert.Contains("🎯 Recommendations", html);
    }



    [Fact]
    public void GenerateMarkdownReport_ShouldGenerateValidMarkdown()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 0),
            ProjectFiles = ["Project.csproj", "README.md"],
            SourceFiles = ["Program.cs", "Startup.cs"],
            Handlers = [new HandlerInfo { Name = "TestHandler" }],
            Requests = [new RequestInfo { Name = "TestRequest" }],
            PerformanceIssues = [
                new PerformanceIssue { Description = "Memory leak", Severity = "High", Recommendation = "Fix memory leak", PotentialImprovement = "Reduce memory usage" }
            ],
            Recommendations = [
                new Recommendation { Title = "Add caching", Description = "Implement caching", Priority = "High", Actions = ["Add cache layer"], EstimatedImpact = "Improve performance" }
            ]
        };

        // Act
        var markdown = ReportGenerator.GenerateMarkdownReport(analysis);

        // Assert
        Assert.Contains("# 🔍 Relay Project Analysis Report", markdown);
        Assert.Contains("Generated: 2023-10-15 14:30:00", markdown);
        Assert.Contains("## Overall Score:", markdown);
        Assert.Contains("## 📊 Project Overview", markdown);
        Assert.Contains("- Project Files: 2", markdown);
        Assert.Contains("- Source Files: 2", markdown);
        Assert.Contains("- Handlers Found: 1", markdown);
        Assert.Contains("- Requests Found: 1", markdown);
        Assert.Contains("## ⚡ Performance Issues", markdown);
        Assert.Contains("### Memory leak", markdown);
        Assert.Contains("**Severity:** High", markdown);
        Assert.Contains("## 🎯 Recommendations", markdown);
        Assert.Contains("### Add caching", markdown);
        Assert.Contains("**Priority:** High", markdown);
    }

    [Fact]
    public void GenerateMarkdownReport_ShouldIncludeOverallScore()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 0),
            HasRelayCore = true,
            HasLogging = true,
            PerformanceIssues = [
                new PerformanceIssue { Severity = "High" }
            ]
        };

        // Act
        var markdown = ReportGenerator.GenerateMarkdownReport(analysis);

        // Assert
        Assert.Contains("## Overall Score: 8.8/10", markdown);
    }

    [Fact]
    public void GenerateMarkdownReport_ShouldHandleEmptyCollections()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 0),
            ProjectFiles = [],
            SourceFiles = [],
            Handlers = [],
            Requests = [],
            PerformanceIssues = [],
            Recommendations = []
        };

        // Act
        var markdown = ReportGenerator.GenerateMarkdownReport(analysis);

        // Assert
        Assert.Contains("- Project Files: 0", markdown);
        Assert.Contains("- Source Files: 0", markdown);
        Assert.Contains("- Handlers Found: 0", markdown);
        Assert.Contains("- Requests Found: 0", markdown);
        Assert.DoesNotContain("## ⚡ Performance Issues", markdown);
        Assert.DoesNotContain("## 🎯 Recommendations", markdown);
    }



    [Fact]
    public void GenerateHtmlAnalysisReport_ShouldStyleIssuesBySeverity()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 0),
            PerformanceIssues = [
                new PerformanceIssue { Description = "High issue", Severity = "High" },
                new PerformanceIssue { Description = "Medium issue", Severity = "Medium" },
                new PerformanceIssue { Description = "Low issue", Severity = "Low" }
            ]
        };

        // Act
        var html = ReportGenerator.GenerateHtmlAnalysisReport(analysis);

        // Assert
        Assert.Contains("class='issue high'", html);
        Assert.Contains("class='issue medium'", html);
        Assert.Contains("class='issue low'", html);
    }

    [Fact]
    public void GenerateMarkdownReport_ShouldFormatRecommendationsWithActions()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Timestamp = new DateTime(2023, 10, 15, 14, 30, 0),
            Recommendations = [
                new Recommendation {
                    Title = "Test recommendation",
                    Description = "Test description",
                    Priority = "High",
                    Actions = ["Action 1", "Action 2"],
                    EstimatedImpact = "High impact"
                }
            ]
        };

        // Act
        var markdown = ReportGenerator.GenerateMarkdownReport(analysis);

        // Assert
        Assert.Contains("### Test recommendation", markdown);
        Assert.Contains("**Priority:** High", markdown);
        Assert.Contains("**Description:** Test description", markdown);
        Assert.Contains("**Actions:**", markdown);
        Assert.Contains("- Action 1", markdown);
        Assert.Contains("- Action 2", markdown);
        Assert.Contains("**Estimated Impact:** High impact", markdown);
    }

    [Fact]
    public void GenerateHtmlAnalysisReport_ShouldHandleExceptionsAndReturnMinimalHtml()
    {
        // Arrange - Create analysis with null collections to trigger NullReferenceException
        var analysis = new ProjectAnalysis
        {
            Timestamp = DateTime.Now,
            ProjectFiles = null!, // This will cause exception when accessing .Count
            SourceFiles = new List<string>(),
            Handlers = new List<HandlerInfo>(),
            Requests = new List<RequestInfo>(),
            PerformanceIssues = new List<PerformanceIssue>(),
            Recommendations = new List<Recommendation>()
        };

        // Act
        var html = ReportGenerator.GenerateHtmlAnalysisReport(analysis);

        // Assert - Should return minimal HTML instead of throwing
        Assert.Contains("<html>", html);
        Assert.Contains("<body>", html);
        Assert.Contains("<h1>Relay Project Analysis Report</h1>", html);
        Assert.Contains("Report generation failed:", html);
    }
}