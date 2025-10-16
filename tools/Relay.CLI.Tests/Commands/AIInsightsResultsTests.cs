using Relay.CLI.Commands;
using System.Text.Json;

namespace Relay.CLI.Tests.Commands;

public class AIInsightsResultsTests
{
    [Fact]
    public void AIInsightsResults_ShouldHaveHealthScoreProperty()
    {
        // Arrange & Act
        var results = new AIInsightsResults { HealthScore = 85.7 };

        // Assert
        results.HealthScore.Should().Be(85.7);
    }

    [Fact]
    public void AIInsightsResults_ShouldHavePerformanceGradeProperty()
    {
        // Arrange & Act
        var results = new AIInsightsResults { PerformanceGrade = 'A' };

        // Assert
        results.PerformanceGrade.Should().Be('A');
    }

    [Fact]
    public void AIInsightsResults_ShouldHaveReliabilityScoreProperty()
    {
        // Arrange & Act
        var results = new AIInsightsResults { ReliabilityScore = 92.3 };

        // Assert
        results.ReliabilityScore.Should().Be(92.3);
    }

    [Fact]
    public void AIInsightsResults_ShouldHaveCriticalIssuesProperty()
    {
        // Arrange
        var issues = new[] { "Memory leak detected", "Database connection timeout" };

        // Act
        var results = new AIInsightsResults { CriticalIssues = issues };

        // Assert
        results.CriticalIssues.Should().BeEquivalentTo(issues);
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
        results.OptimizationOpportunities.Should().HaveCount(2);
        results.OptimizationOpportunities[0].Strategy.Should().Be("Caching");
        results.OptimizationOpportunities[1].ExpectedImprovement.Should().Be(15.0);
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
        results.Predictions.Should().HaveCount(2);
        results.Predictions[0].Metric.Should().Be("Throughput");
        results.Predictions[1].PredictedValue.Should().Be("95ms");
    }

    [Fact]
    public void AIInsightsResults_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var results = new AIInsightsResults();

        // Assert
        results.HealthScore.Should().Be(0.0);
        results.PerformanceGrade.Should().Be('\0');
        results.ReliabilityScore.Should().Be(0.0);
        results.CriticalIssues.Should().BeEmpty();
        results.OptimizationOpportunities.Should().BeEmpty();
        results.Predictions.Should().BeEmpty();
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
        results.HealthScore.Should().Be(78.5);
        results.PerformanceGrade.Should().Be('B');
        results.ReliabilityScore.Should().Be(89.2);
        results.CriticalIssues.Should().BeEquivalentTo(issues);
        results.OptimizationOpportunities.Should().HaveCount(1);
        results.Predictions.Should().HaveCount(1);
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
        results.HealthScore.Should().Be(0.0);
        results.ReliabilityScore.Should().Be(0.0);
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
        results.HealthScore.Should().Be(100.0);
        results.ReliabilityScore.Should().Be(100.0);
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
            results.PerformanceGrade.Should().Be(grade);
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
        deserialized.Should().NotBeNull();
        deserialized!.HealthScore.Should().Be(85.0);
        deserialized.PerformanceGrade.Should().Be('A');
        deserialized.ReliabilityScore.Should().Be(90.5);
        deserialized.CriticalIssues.Should().HaveCount(2);
        deserialized.OptimizationOpportunities.Should().HaveCount(1);
        deserialized.Predictions.Should().HaveCount(1);
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
        results.Should().NotBeNull();
        results!.HealthScore.Should().Be(92.3);
        results.PerformanceGrade.Should().Be('B');
        results.ReliabilityScore.Should().Be(87.6);
        results.CriticalIssues.Should().HaveCount(2);
        results.OptimizationOpportunities.Should().HaveCount(1);
        results.Predictions.Should().HaveCount(1);
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
        results.CriticalIssues.Should().BeEmpty();
        results.OptimizationOpportunities.Should().BeEmpty();
        results.Predictions.Should().BeEmpty();
    }

    [Fact]
    public void AIInsightsResults_ShouldHandleNullArraysGracefully()
    {
        // Arrange & Act
        var results = new AIInsightsResults();

        // Assert - Default initialization should provide empty arrays
        results.CriticalIssues.Should().NotBeNull();
        results.OptimizationOpportunities.Should().NotBeNull();
        results.Predictions.Should().NotBeNull();
        results.CriticalIssues.Should().BeEmpty();
        results.OptimizationOpportunities.Should().BeEmpty();
        results.Predictions.Should().BeEmpty();
    }
}