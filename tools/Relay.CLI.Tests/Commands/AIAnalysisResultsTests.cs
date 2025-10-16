using Relay.CLI.Commands;
using System.Text.Json;

namespace Relay.CLI.Tests.Commands;

public class AIAnalysisResultsTests
{
    [Fact]
    public void AIAnalysisResults_ShouldHaveProjectPathProperty()
    {
        // Arrange & Act
        var results = new AIAnalysisResults { ProjectPath = "/path/to/project" };

        // Assert
        results.ProjectPath.Should().Be("/path/to/project");
    }

    [Fact]
    public void AIAnalysisResults_ShouldHaveFilesAnalyzedProperty()
    {
        // Arrange & Act
        var results = new AIAnalysisResults { FilesAnalyzed = 42 };

        // Assert
        results.FilesAnalyzed.Should().Be(42);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHaveHandlersFoundProperty()
    {
        // Arrange & Act
        var results = new AIAnalysisResults { HandlersFound = 15 };

        // Assert
        results.HandlersFound.Should().Be(15);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHavePerformanceScoreProperty()
    {
        // Arrange & Act
        var results = new AIAnalysisResults { PerformanceScore = 85.7 };

        // Assert
        results.PerformanceScore.Should().Be(85.7);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHaveAIConfidenceProperty()
    {
        // Arrange & Act
        var results = new AIAnalysisResults { AIConfidence = 0.92 };

        // Assert
        results.AIConfidence.Should().Be(0.92);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHavePerformanceIssuesProperty()
    {
        // Arrange
        var issues = new[]
        {
            new AIPerformanceIssue { Severity = "High", Description = "Memory leak", Location = "Service.cs", Impact = "Critical" },
            new AIPerformanceIssue { Severity = "Medium", Description = "Inefficient query", Location = "Repository.cs", Impact = "Moderate" }
        };

        // Act
        var results = new AIAnalysisResults { PerformanceIssues = issues };

        // Assert
        results.PerformanceIssues.Should().HaveCount(2);
        results.PerformanceIssues[0].Severity.Should().Be("High");
        results.PerformanceIssues[1].Description.Should().Be("Inefficient query");
    }

    [Fact]
    public void AIAnalysisResults_ShouldHaveOptimizationOpportunitiesProperty()
    {
        // Arrange
        var opportunities = new[]
        {
            new OptimizationOpportunity { Strategy = "Caching", ExpectedImprovement = 25.0, Confidence = 0.8 },
            new OptimizationOpportunity { Strategy = "Async", ExpectedImprovement = 15.0, Confidence = 0.9 }
        };

        // Act
        var results = new AIAnalysisResults { OptimizationOpportunities = opportunities };

        // Assert
        results.OptimizationOpportunities.Should().HaveCount(2);
        results.OptimizationOpportunities[0].Strategy.Should().Be("Caching");
        results.OptimizationOpportunities[1].ExpectedImprovement.Should().Be(15.0);
    }

    [Fact]
    public void AIAnalysisResults_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var results = new AIAnalysisResults();

        // Assert
        results.ProjectPath.Should().Be("");
        results.FilesAnalyzed.Should().Be(0);
        results.HandlersFound.Should().Be(0);
        results.PerformanceScore.Should().Be(0.0);
        results.AIConfidence.Should().Be(0.0);
        results.PerformanceIssues.Should().BeEmpty();
        results.OptimizationOpportunities.Should().BeEmpty();
    }

    [Fact]
    public void AIAnalysisResults_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var issues = new[]
        {
            new AIPerformanceIssue
            {
                Severity = "Critical",
                Description = "Database connection timeout",
                Location = "DataAccess.cs:45",
                Impact = "High"
            }
        };
        var opportunities = new[]
        {
            new OptimizationOpportunity
            {
                Strategy = "ConnectionPooling",
                Description = "Implement connection pooling",
                ExpectedImprovement = 30.0,
                Confidence = 0.95,
                RiskLevel = "Low",
                Title = "Database Optimization"
            }
        };

        // Act
        var results = new AIAnalysisResults
        {
            ProjectPath = "/src/MyProject",
            FilesAnalyzed = 150,
            HandlersFound = 25,
            PerformanceScore = 78.5,
            AIConfidence = 0.87,
            PerformanceIssues = issues,
            OptimizationOpportunities = opportunities
        };

        // Assert
        results.ProjectPath.Should().Be("/src/MyProject");
        results.FilesAnalyzed.Should().Be(150);
        results.HandlersFound.Should().Be(25);
        results.PerformanceScore.Should().Be(78.5);
        results.AIConfidence.Should().Be(0.87);
        results.PerformanceIssues.Should().HaveCount(1);
        results.OptimizationOpportunities.Should().HaveCount(1);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHandleZeroValues()
    {
        // Arrange & Act
        var results = new AIAnalysisResults
        {
            FilesAnalyzed = 0,
            HandlersFound = 0,
            PerformanceScore = 0.0,
            AIConfidence = 0.0
        };

        // Assert
        results.FilesAnalyzed.Should().Be(0);
        results.HandlersFound.Should().Be(0);
        results.PerformanceScore.Should().Be(0.0);
        results.AIConfidence.Should().Be(0.0);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHandleHighValues()
    {
        // Arrange & Act
        var results = new AIAnalysisResults
        {
            FilesAnalyzed = 10000,
            HandlersFound = 5000,
            PerformanceScore = 100.0,
            AIConfidence = 1.0
        };

        // Assert
        results.FilesAnalyzed.Should().Be(10000);
        results.HandlersFound.Should().Be(5000);
        results.PerformanceScore.Should().Be(100.0);
        results.AIConfidence.Should().Be(1.0);
    }

    [Fact]
    public void AIAnalysisResults_ShouldSerializeToJson()
    {
        // Arrange
        var results = new AIAnalysisResults
        {
            ProjectPath = "/project",
            FilesAnalyzed = 100,
            HandlersFound = 20,
            PerformanceScore = 85.0,
            AIConfidence = 0.9,
            PerformanceIssues = new[]
            {
                new AIPerformanceIssue { Severity = "High", Description = "Issue", Location = "File.cs", Impact = "High" }
            },
            OptimizationOpportunities = new[]
            {
                new OptimizationOpportunity { Strategy = "Cache", ExpectedImprovement = 20.0, Confidence = 0.8 }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(results);
        var deserialized = JsonSerializer.Deserialize<AIAnalysisResults>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ProjectPath.Should().Be("/project");
        deserialized.FilesAnalyzed.Should().Be(100);
        deserialized.HandlersFound.Should().Be(20);
        deserialized.PerformanceScore.Should().Be(85.0);
        deserialized.AIConfidence.Should().Be(0.9);
        deserialized.PerformanceIssues.Should().HaveCount(1);
        deserialized.OptimizationOpportunities.Should().HaveCount(1);
    }

    [Fact]
    public void AIAnalysisResults_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""ProjectPath"": ""/test/project"",
            ""FilesAnalyzed"": 75,
            ""HandlersFound"": 12,
            ""PerformanceScore"": 92.3,
            ""AIConfidence"": 0.85,
            ""PerformanceIssues"": [
                {
                    ""Severity"": ""Medium"",
                    ""Description"": ""Slow query detected"",
                    ""Location"": ""QueryHandler.cs:120"",
                    ""Impact"": ""Medium""
                }
            ],
            ""OptimizationOpportunities"": [
                {
                    ""Strategy"": ""Indexing"",
                    ""Description"": ""Add database indexes"",
                    ""ExpectedImprovement"": 35.0,
                    ""Confidence"": 0.88,
                    ""RiskLevel"": ""Low"",
                    ""Title"": ""Database Indexing""
                }
            ]
        }";

        // Act
        var results = JsonSerializer.Deserialize<AIAnalysisResults>(json);

        // Assert
        results.Should().NotBeNull();
        results!.ProjectPath.Should().Be("/test/project");
        results.FilesAnalyzed.Should().Be(75);
        results.HandlersFound.Should().Be(12);
        results.PerformanceScore.Should().Be(92.3);
        results.AIConfidence.Should().Be(0.85);
        results.PerformanceIssues.Should().HaveCount(1);
        results.OptimizationOpportunities.Should().HaveCount(1);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHandleEmptyArrays()
    {
        // Arrange & Act
        var results = new AIAnalysisResults
        {
            PerformanceIssues = Array.Empty<AIPerformanceIssue>(),
            OptimizationOpportunities = Array.Empty<OptimizationOpportunity>()
        };

        // Assert
        results.PerformanceIssues.Should().BeEmpty();
        results.OptimizationOpportunities.Should().BeEmpty();
    }

    [Fact]
    public void AIAnalysisResults_ShouldHandleNullArraysGracefully()
    {
        // Arrange & Act
        var results = new AIAnalysisResults();

        // Assert - Default initialization should provide empty arrays
        results.PerformanceIssues.Should().NotBeNull();
        results.OptimizationOpportunities.Should().NotBeNull();
        results.PerformanceIssues.Should().BeEmpty();
        results.OptimizationOpportunities.Should().BeEmpty();
    }
}