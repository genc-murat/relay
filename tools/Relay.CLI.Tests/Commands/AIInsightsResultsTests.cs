using Relay.CLI.Commands;
using System.Text.Json;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class AIInsightsResultsTests
{
    [Fact]
    public void AIInsightsResults_ShouldHaveHealthScoreProperty()
    {
        // Arrange & Act
        var results = new AIInsightsResults { HealthScore = 85.7 };

        // Assert
        Assert.Equal(85.7, results.HealthScore);
    }

    [Fact]
    public void AIInsightsResults_ShouldHavePerformanceGradeProperty()
    {
        // Arrange & Act
        var results = new AIInsightsResults { PerformanceGrade = 'A' };

        // Assert
        Assert.Equal('A', results.PerformanceGrade);
    }

    [Fact]
    public void AIInsightsResults_ShouldHaveReliabilityScoreProperty()
    {
        // Arrange & Act
        var results = new AIInsightsResults { ReliabilityScore = 92.3 };

        // Assert
        Assert.Equal(92.3, results.ReliabilityScore);
    }

    [Fact]
    public void AIInsightsResults_ShouldHaveCriticalIssuesProperty()
    {
        // Arrange
        var issues = new[] { "Memory leak detected", "Database connection timeout" };

        // Act
        var results = new AIInsightsResults { CriticalIssues = issues };

        // Assert
        Assert.Equal(issues, results.CriticalIssues);
    }

    [Fact]
    public void AIInsightsResults_ShouldHaveOptimizationOpportunitiesProperty()
    {
        // Arrange
        var opportunities = new[]
        {
            new OptimizationOpportunity { Strategy = "Caching", ExpectedImprovement = 25.0, Confidence = 0.8 },
            new OptimizationOpportunity { Strategy = "Async", ExpectedImprovement = 15.0, Confidence = 0.9 }
        };

        // Act
        var results = new AIInsightsResults { OptimizationOpportunities = opportunities };

        // Assert
        Assert.Equal(2, results.OptimizationOpportunities.Length);
        Assert.Equal("Caching", results.OptimizationOpportunities[0].Strategy);
        Assert.Equal(15.0, results.OptimizationOpportunities[1].ExpectedImprovement);
    }

    [Fact]
    public void AIInsightsResults_ShouldHavePredictionsProperty()
    {
        // Arrange
        var predictions = new[]
        {
            new PredictionResult { Metric = "Throughput", PredictedValue = "1,200 req/sec", Confidence = 0.89 },
            new PredictionResult { Metric = "Response Time", PredictedValue = "95ms", Confidence = 0.92 }
        };

        // Act
        var results = new AIInsightsResults { Predictions = predictions };

        // Assert
        Assert.Equal(2, results.Predictions.Length);
        Assert.Equal("Throughput", results.Predictions[0].Metric);
        Assert.Equal("95ms", results.Predictions[1].PredictedValue);
    }

    [Fact]
    public void AIInsightsResults_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var results = new AIInsightsResults();

        // Assert
        Assert.Equal(0.0, results.HealthScore);
        Assert.Equal('\0', results.PerformanceGrade);
        Assert.Equal(0.0, results.ReliabilityScore);
        Assert.Empty(results.CriticalIssues);
        Assert.Empty(results.OptimizationOpportunities);
        Assert.Empty(results.Predictions);
    }

    [Fact]
    public void AIInsightsResults_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var issues = new[] { "High CPU usage", "Memory leak" };
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
        var predictions = new[]
        {
            new PredictionResult { Metric = "Error Rate", PredictedValue = "0.5%", Confidence = 0.87 }
        };

        // Act
        var results = new AIInsightsResults
        {
            HealthScore = 78.5,
            PerformanceGrade = 'B',
            ReliabilityScore = 89.2,
            CriticalIssues = issues,
            OptimizationOpportunities = opportunities,
            Predictions = predictions
        };

        // Assert
        Assert.Equal(78.5, results.HealthScore);
        Assert.Equal('B', results.PerformanceGrade);
        Assert.Equal(89.2, results.ReliabilityScore);
        Assert.Equal(issues, results.CriticalIssues);
        Assert.Single(results.OptimizationOpportunities);
        Assert.Single(results.Predictions);
    }

    [Fact]
    public void AIInsightsResults_ShouldHandleZeroValues()
    {
        // Arrange & Act
        var results = new AIInsightsResults
        {
            HealthScore = 0.0,
            ReliabilityScore = 0.0
        };

        // Assert
        Assert.Equal(0.0, results.HealthScore);
        Assert.Equal(0.0, results.ReliabilityScore);
    }

    [Fact]
    public void AIInsightsResults_ShouldHandleHighValues()
    {
        // Arrange & Act
        var results = new AIInsightsResults
        {
            HealthScore = 100.0,
            ReliabilityScore = 100.0
        };

        // Assert
        Assert.Equal(100.0, results.HealthScore);
        Assert.Equal(100.0, results.ReliabilityScore);
    }

    [Fact]
    public void AIInsightsResults_ShouldHandleDifferentPerformanceGrades()
    {
        // Arrange
        var grades = new[] { 'A', 'B', 'C', 'D', 'F' };

        foreach (var grade in grades)
        {
            // Act
            var results = new AIInsightsResults { PerformanceGrade = grade };

            // Assert
            Assert.Equal(grade, results.PerformanceGrade);
        }
    }

    [Fact]
    public void AIInsightsResults_ShouldSerializeToJson()
    {
        // Arrange
        var results = new AIInsightsResults
        {
            HealthScore = 85.0,
            PerformanceGrade = 'A',
            ReliabilityScore = 90.5,
            CriticalIssues = new[] { "Memory issue", "Performance bottleneck" },
            OptimizationOpportunities = new[]
            {
                new OptimizationOpportunity { Strategy = "Cache", ExpectedImprovement = 20.0, Confidence = 0.8 }
            },
            Predictions = new[]
            {
                new PredictionResult { Metric = "Throughput", PredictedValue = "1000 req/sec", Confidence = 0.9 }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(results);
        var deserialized = JsonSerializer.Deserialize<AIInsightsResults>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(85.0, deserialized!.HealthScore);
        Assert.Equal('A', deserialized.PerformanceGrade);
        Assert.Equal(90.5, deserialized.ReliabilityScore);
        Assert.Equal(2, deserialized.CriticalIssues.Length);
        Assert.Single(deserialized.OptimizationOpportunities);
        Assert.Single(deserialized.Predictions);
    }

    [Fact]
    public void AIInsightsResults_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""HealthScore"": 92.3,
            ""PerformanceGrade"": ""B"",
            ""ReliabilityScore"": 87.6,
            ""CriticalIssues"": [""Slow query detected"", ""High memory usage""],
            ""OptimizationOpportunities"": [
                {
                    ""Strategy"": ""Indexing"",
                    ""Description"": ""Add database indexes"",
                    ""ExpectedImprovement"": 35.0,
                    ""Confidence"": 0.88,
                    ""RiskLevel"": ""Low"",
                    ""Title"": ""Database Indexing""
                }
            ],
            ""Predictions"": [
                {
                    ""Metric"": ""Response Time"",
                    ""PredictedValue"": ""120ms"",
                    ""Confidence"": 0.85
                }
            ]
        }";

        // Act
        var results = JsonSerializer.Deserialize<AIInsightsResults>(json);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(92.3, results!.HealthScore);
        Assert.Equal('B', results.PerformanceGrade);
        Assert.Equal(87.6, results.ReliabilityScore);
        Assert.Equal(2, results.CriticalIssues.Length);
        Assert.Single(results.OptimizationOpportunities);
        Assert.Single(results.Predictions);
    }

    [Fact]
    public void AIInsightsResults_ShouldHandleEmptyArrays()
    {
        // Arrange & Act
        var results = new AIInsightsResults
        {
            CriticalIssues = Array.Empty<string>(),
            OptimizationOpportunities = Array.Empty<OptimizationOpportunity>(),
            Predictions = Array.Empty<PredictionResult>()
        };

        // Assert
        Assert.Empty(results.CriticalIssues);
        Assert.Empty(results.OptimizationOpportunities);
        Assert.Empty(results.Predictions);
    }

    [Fact]
    public void AIInsightsResults_ShouldHandleNullArraysGracefully()
    {
        // Arrange & Act
        var results = new AIInsightsResults();

        // Assert - Default initialization should provide empty arrays
        Assert.NotNull(results.CriticalIssues);
        Assert.NotNull(results.OptimizationOpportunities);
        Assert.NotNull(results.Predictions);
        Assert.Empty(results.CriticalIssues);
        Assert.Empty(results.OptimizationOpportunities);
        Assert.Empty(results.Predictions);
    }
}
