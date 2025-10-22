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
        Assert.Equal("/path/to/project", results.ProjectPath);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHaveFilesAnalyzedProperty()
    {
        // Arrange & Act
        var results = new AIAnalysisResults { FilesAnalyzed = 42 };

        // Assert
        Assert.Equal(42, results.FilesAnalyzed);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHaveHandlersFoundProperty()
    {
        // Arrange & Act
        var results = new AIAnalysisResults { HandlersFound = 15 };

        // Assert
        Assert.Equal(15, results.HandlersFound);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHavePerformanceScoreProperty()
    {
        // Arrange & Act
        var results = new AIAnalysisResults { PerformanceScore = 85.7 };

        // Assert
        Assert.Equal(85.7, results.PerformanceScore);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHaveAIConfidenceProperty()
    {
        // Arrange & Act
        var results = new AIAnalysisResults { AIConfidence = 0.92 };

        // Assert
        Assert.Equal(0.92, results.AIConfidence);
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
        Assert.Equal(2, results.PerformanceIssues.Length);
        Assert.Equal("High", results.PerformanceIssues[0].Severity);
        Assert.Equal("Inefficient query", results.PerformanceIssues[1].Description);
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
        Assert.Equal(2, results.OptimizationOpportunities.Length);
        Assert.Equal("Caching", results.OptimizationOpportunities[0].Strategy);
        Assert.Equal(15.0, results.OptimizationOpportunities[1].ExpectedImprovement);
    }

    [Fact]
    public void AIAnalysisResults_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var results = new AIAnalysisResults();

        // Assert
        Assert.Equal("", results.ProjectPath);
        Assert.Equal(0, results.FilesAnalyzed);
        Assert.Equal(0, results.HandlersFound);
        Assert.Equal(0.0, results.PerformanceScore);
        Assert.Equal(0.0, results.AIConfidence);
        Assert.Empty(results.PerformanceIssues);
        Assert.Empty(results.OptimizationOpportunities);
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
        Assert.Equal("/src/MyProject", results.ProjectPath);
        Assert.Equal(150, results.FilesAnalyzed);
        Assert.Equal(25, results.HandlersFound);
        Assert.Equal(78.5, results.PerformanceScore);
        Assert.Equal(0.87, results.AIConfidence);
        Assert.Single(results.PerformanceIssues);
        Assert.Single(results.OptimizationOpportunities);
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
        Assert.Equal(0, results.FilesAnalyzed);
        Assert.Equal(0, results.HandlersFound);
        Assert.Equal(0.0, results.PerformanceScore);
        Assert.Equal(0.0, results.AIConfidence);
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
        Assert.Equal(10000, results.FilesAnalyzed);
        Assert.Equal(5000, results.HandlersFound);
        Assert.Equal(100.0, results.PerformanceScore);
        Assert.Equal(1.0, results.AIConfidence);
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
            PerformanceIssues =
            [
                new AIPerformanceIssue { Severity = "High", Description = "Issue", Location = "File.cs", Impact = "High" }
            ],
            OptimizationOpportunities =
            [
                new OptimizationOpportunity { Strategy = "Cache", ExpectedImprovement = 20.0, Confidence = 0.8 }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(results);
        var deserialized = JsonSerializer.Deserialize<AIAnalysisResults>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("/project", deserialized.ProjectPath);
        Assert.Equal(100, deserialized.FilesAnalyzed);
        Assert.Equal(20, deserialized.HandlersFound);
        Assert.Equal(85.0, deserialized.PerformanceScore);
        Assert.Equal(0.9, deserialized.AIConfidence);
        Assert.Single(deserialized.PerformanceIssues);
        Assert.Single(deserialized.OptimizationOpportunities);
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
        Assert.NotNull(results);
        Assert.Equal("/test/project", results.ProjectPath);
        Assert.Equal(75, results.FilesAnalyzed);
        Assert.Equal(12, results.HandlersFound);
        Assert.Equal(92.3, results.PerformanceScore);
        Assert.Equal(0.85, results.AIConfidence);
        Assert.Single(results.PerformanceIssues);
        Assert.Single(results.OptimizationOpportunities);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHandleEmptyArrays()
    {
        // Arrange & Act
        var results = new AIAnalysisResults
        {
            PerformanceIssues = [],
            OptimizationOpportunities = []
        };

        // Assert
        Assert.Empty(results.PerformanceIssues);
        Assert.Empty(results.OptimizationOpportunities);
    }

    [Fact]
    public void AIAnalysisResults_ShouldHandleNullArraysGracefully()
    {
        // Arrange & Act
        var results = new AIAnalysisResults();

        // Assert - Default initialization should provide empty arrays
        Assert.NotNull(results.PerformanceIssues);
        Assert.NotNull(results.OptimizationOpportunities);
        Assert.Empty(results.PerformanceIssues);
        Assert.Empty(results.OptimizationOpportunities);
    }
}
