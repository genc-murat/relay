using Relay.CLI.Commands;

using System.Threading.Tasks;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class AIInsightsGeneratorTests
{
    private readonly AIInsightsGenerator _insightsGenerator;

    public AIInsightsGeneratorTests()
    {
        _insightsGenerator = new AIInsightsGenerator();
    }

    [Fact]
    public async Task GenerateInsightsAsync_WithValidParameters_ReturnsInsightsResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<AIInsightsResults>(result);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsExpectedHealthScore()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.Equal(8.2, result.HealthScore);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsValidHealthScoreRange()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.True(result.HealthScore >= 0);
        Assert.True(result.HealthScore <= 10);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsExpectedPerformanceGrade()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.Equal('B', result.PerformanceGrade);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsExpectedReliabilityScore()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.Equal(9.1, result.ReliabilityScore);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsValidReliabilityScoreRange()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.True(result.ReliabilityScore >= 0);
        Assert.True(result.ReliabilityScore <= 10);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsCriticalIssues()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result.CriticalIssues);
        Assert.Single(result.CriticalIssues);
        Assert.Equal("High memory usage detected in order processing", result.CriticalIssues[0]);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsOptimizationOpportunities()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result.OptimizationOpportunities);
        Assert.Equal(2, result.OptimizationOpportunities.Count());

        var cachingOpportunity = result.OptimizationOpportunities.FirstOrDefault(o => o.Title == "Enable Caching");
        Assert.NotNull(cachingOpportunity);
        Assert.Equal("Enable Caching", cachingOpportunity.Title);
        Assert.Equal(0.4, cachingOpportunity.ExpectedImprovement);

        var queryOpportunity = result.OptimizationOpportunities.FirstOrDefault(o => o.Title == "Optimize Database Queries");
        Assert.NotNull(queryOpportunity);
        Assert.Equal("Optimize Database Queries", queryOpportunity.Title);
        Assert.Equal(0.25, queryOpportunity.ExpectedImprovement);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsPredictions()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result.Predictions);
        Assert.Equal(2, result.Predictions.Count());

        var throughputPrediction = result.Predictions.FirstOrDefault(p => p.Metric == "Throughput");
        Assert.NotNull(throughputPrediction);
        Assert.Equal("Throughput", throughputPrediction.Metric);
        Assert.Equal("1,200 req/sec", throughputPrediction.PredictedValue);
        Assert.Equal(0.89, throughputPrediction.Confidence);

        var responseTimePrediction = result.Predictions.FirstOrDefault(p => p.Metric == "Response Time");
        Assert.NotNull(responseTimePrediction);
        Assert.Equal("Response Time", responseTimePrediction.Metric);
        Assert.Equal("95ms avg", responseTimePrediction.PredictedValue);
        Assert.Equal(0.92, responseTimePrediction.Confidence);
    }

    [Fact]
    public async Task GenerateInsightsAsync_OptimizationOpportunitiesHaveValidExpectedImprovement()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        foreach (var opportunity in result.OptimizationOpportunities)
        {
            Assert.True(opportunity.ExpectedImprovement > 0);
            Assert.True(opportunity.ExpectedImprovement <= 1);
        }
    }

    [Fact]
    public async Task GenerateInsightsAsync_PredictionsHaveValidConfidence()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        foreach (var prediction in result.Predictions)
        {
            Assert.True(prediction.Confidence > 0);
            Assert.True(prediction.Confidence <= 1);
        }
    }

    [Fact]
    public async Task GenerateInsightsAsync_WithDifferentTimeWindows_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindows = new[] { "1h", "6h", "24h", "7d", "30d" };
        var includeHealth = true;
        var includePredictions = true;

        // Act & Assert
        foreach (var timeWindow in timeWindows)
        {
            var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);
            Assert.NotNull(result);
            // Currently implementation doesn't use timeWindow parameter, so results are the same
            Assert.Equal(8.2, result.HealthScore);
            Assert.Equal('B', result.PerformanceGrade);
        }
    }

    [Fact]
    public async Task GenerateInsightsAsync_WithIncludeHealthFalse_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = false;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result);
        // Currently implementation doesn't use includeHealth parameter, so results are the same
        Assert.Equal(8.2, result.HealthScore);
        Assert.Equal(9.1, result.ReliabilityScore);
    }

    [Fact]
    public async Task GenerateInsightsAsync_WithIncludePredictionsFalse_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = false;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result);
        // Currently implementation doesn't use includePredictions parameter, so results are the same
        Assert.Equal(2, result.Predictions.Count());
    }

    [Fact]
    public async Task GenerateInsightsAsync_WithBothFlagsFalse_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = false;
        var includePredictions = false;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result);
        // Currently implementation doesn't use the flags, so results are the same
        Assert.Equal(8.2, result.HealthScore);
        Assert.Equal(2, result.Predictions.Count());
    }

    [Fact]
    public async Task GenerateInsightsAsync_CompletesWithinReasonableTime()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);
        stopwatch.Stop();

        // Assert
        // Should complete in less than 3 seconds (simulated delay is 2.5 seconds)
        Assert.True(stopwatch.ElapsedMilliseconds < 3000);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsNonEmptyCollections()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotEmpty(result.CriticalIssues);
        Assert.NotEmpty(result.OptimizationOpportunities);
        Assert.NotEmpty(result.Predictions);
    }

    [Fact]
    public async Task GenerateInsightsAsync_CriticalIssuesAreNonEmptyStrings()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        foreach (var issue in result.CriticalIssues)
        {
            Assert.False(string.IsNullOrEmpty(issue));
        }
    }

    [Fact]
    public async Task GenerateInsightsAsync_OptimizationOpportunitiesHaveRequiredProperties()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        foreach (var opportunity in result.OptimizationOpportunities)
        {
            Assert.False(string.IsNullOrEmpty(opportunity.Title));
            Assert.True(opportunity.ExpectedImprovement > 0);
        }
    }

    [Fact]
    public async Task GenerateInsightsAsync_PredictionsHaveRequiredProperties()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        foreach (var prediction in result.Predictions)
        {
            Assert.False(string.IsNullOrEmpty(prediction.Metric));
            Assert.False(string.IsNullOrEmpty(prediction.PredictedValue));
            Assert.True(prediction.Confidence > 0);
        }
    }

    [Fact]
    public async Task GenerateInsightsAsync_PerformanceGradeIsValidLetter()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        var validGrades = new[] { 'A', 'B', 'C', 'D', 'F' };
        Assert.Contains(result.PerformanceGrade, validGrades);
    }

    [Fact]
    public async Task GenerateInsightsAsync_AllScoresAreWithinValidRanges()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.InRange(result.HealthScore, 0, 10);
        Assert.InRange(result.ReliabilityScore, 0, 10);
    }

    [Fact]
    public async Task GenerateInsightsAsync_WithEmptyPath_ReturnsSameResults()
    {
        // Arrange
        var path = "";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result);
        // Currently implementation doesn't use path parameter, so results are the same
        Assert.Equal(8.2, result.HealthScore);
    }

    [Fact]
    public async Task GenerateInsightsAsync_WithNullPath_ReturnsSameResults()
    {
        // Arrange
        string path = null!;
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result);
        // Currently implementation doesn't use path parameter, so results are the same
        Assert.Equal(8.2, result.HealthScore);
    }

    [Fact]
    public async Task GenerateInsightsAsync_WithEmptyTimeWindow_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.NotNull(result);
        // Currently implementation doesn't use timeWindow parameter, so results are the same
        Assert.Equal(8.2, result.HealthScore);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsExpectedOptimizationTitles()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        var titles = result.OptimizationOpportunities.Select(o => o.Title).ToArray();
        Assert.Contains("Enable Caching", titles);
        Assert.Contains("Optimize Database Queries", titles);
    }

    [Fact]
    public async Task GenerateInsightsAsync_ReturnsExpectedPredictionMetrics()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        var metrics = result.Predictions.Select(p => p.Metric).ToArray();
        Assert.Contains("Throughput", metrics);
        Assert.Contains("Response Time", metrics);
    }

    [Fact]
    public async Task GenerateInsightsAsync_CriticalIssuesContainExpectedContent()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        Assert.Contains(result.CriticalIssues, issue => issue.Contains("memory usage"));
        Assert.Contains(result.CriticalIssues, issue => issue.Contains("order processing"));
    }

    [Fact]
    public async Task GenerateInsightsAsync_PredictedValuesAreNonEmpty()
    {
        // Arrange
        var path = @"C:\test\project";
        var timeWindow = "24h";
        var includeHealth = true;
        var includePredictions = true;

        // Act
        var result = await _insightsGenerator.GenerateInsightsAsync(path, timeWindow, includeHealth, includePredictions);

        // Assert
        foreach (var prediction in result.Predictions)
        {
            Assert.False(string.IsNullOrEmpty(prediction.PredictedValue));
            Assert.True(prediction.PredictedValue.Contains("req/sec") || prediction.PredictedValue.Contains("ms avg"));
        }
    }
}



