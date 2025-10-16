using Relay.CLI.Commands;
using FluentAssertions;
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
        result.Should().NotBeNull();
        result.Should().BeOfType<AIInsightsResults>();
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
        result.HealthScore.Should().Be(8.2);
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
        result.HealthScore.Should().BeGreaterThanOrEqualTo(0);
        result.HealthScore.Should().BeLessThanOrEqualTo(10);
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
        result.PerformanceGrade.Should().Be('B');
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
        result.ReliabilityScore.Should().Be(9.1);
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
        result.ReliabilityScore.Should().BeGreaterThanOrEqualTo(0);
        result.ReliabilityScore.Should().BeLessThanOrEqualTo(10);
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
        result.CriticalIssues.Should().NotBeNull();
        result.CriticalIssues.Should().HaveCount(1);
        result.CriticalIssues[0].Should().Be("High memory usage detected in order processing");
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
        result.OptimizationOpportunities.Should().NotBeNull();
        result.OptimizationOpportunities.Should().HaveCount(2);

        var cachingOpportunity = result.OptimizationOpportunities.FirstOrDefault(o => o.Title == "Enable Caching");
        cachingOpportunity.Should().NotBeNull();
        cachingOpportunity.Title.Should().Be("Enable Caching");
        cachingOpportunity.ExpectedImprovement.Should().Be(0.4);

        var queryOpportunity = result.OptimizationOpportunities.FirstOrDefault(o => o.Title == "Optimize Database Queries");
        queryOpportunity.Should().NotBeNull();
        queryOpportunity.Title.Should().Be("Optimize Database Queries");
        queryOpportunity.ExpectedImprovement.Should().Be(0.25);
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
        result.Predictions.Should().NotBeNull();
        result.Predictions.Should().HaveCount(2);

        var throughputPrediction = result.Predictions.FirstOrDefault(p => p.Metric == "Throughput");
        throughputPrediction.Should().NotBeNull();
        throughputPrediction.Metric.Should().Be("Throughput");
        throughputPrediction.PredictedValue.Should().Be("1,200 req/sec");
        throughputPrediction.Confidence.Should().Be(0.89);

        var responseTimePrediction = result.Predictions.FirstOrDefault(p => p.Metric == "Response Time");
        responseTimePrediction.Should().NotBeNull();
        responseTimePrediction.Metric.Should().Be("Response Time");
        responseTimePrediction.PredictedValue.Should().Be("95ms avg");
        responseTimePrediction.Confidence.Should().Be(0.92);
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
            opportunity.ExpectedImprovement.Should().BeGreaterThan(0);
            opportunity.ExpectedImprovement.Should().BeLessThanOrEqualTo(1);
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
            prediction.Confidence.Should().BeGreaterThan(0);
            prediction.Confidence.Should().BeLessThanOrEqualTo(1);
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
            result.Should().NotBeNull();
            // Currently implementation doesn't use timeWindow parameter, so results are the same
            result.HealthScore.Should().Be(8.2);
            result.PerformanceGrade.Should().Be('B');
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
        result.Should().NotBeNull();
        // Currently implementation doesn't use includeHealth parameter, so results are the same
        result.HealthScore.Should().Be(8.2);
        result.ReliabilityScore.Should().Be(9.1);
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
        result.Should().NotBeNull();
        // Currently implementation doesn't use includePredictions parameter, so results are the same
        result.Predictions.Should().HaveCount(2);
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
        result.Should().NotBeNull();
        // Currently implementation doesn't use the flags, so results are the same
        result.HealthScore.Should().Be(8.2);
        result.Predictions.Should().HaveCount(2);
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
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000);
        result.Should().NotBeNull();
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
        result.CriticalIssues.Should().NotBeEmpty();
        result.OptimizationOpportunities.Should().NotBeEmpty();
        result.Predictions.Should().NotBeEmpty();
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
            issue.Should().NotBeNullOrEmpty();
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
            opportunity.Title.Should().NotBeNullOrEmpty();
            opportunity.ExpectedImprovement.Should().BeGreaterThan(0);
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
            prediction.Metric.Should().NotBeNullOrEmpty();
            prediction.PredictedValue.Should().NotBeNullOrEmpty();
            prediction.Confidence.Should().BeGreaterThan(0);
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
        validGrades.Should().Contain(result.PerformanceGrade);
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
        result.HealthScore.Should().BeInRange(0, 10);
        result.ReliabilityScore.Should().BeInRange(0, 10);
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
        result.Should().NotBeNull();
        // Currently implementation doesn't use path parameter, so results are the same
        result.HealthScore.Should().Be(8.2);
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
        result.Should().NotBeNull();
        // Currently implementation doesn't use path parameter, so results are the same
        result.HealthScore.Should().Be(8.2);
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
        result.Should().NotBeNull();
        // Currently implementation doesn't use timeWindow parameter, so results are the same
        result.HealthScore.Should().Be(8.2);
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
        titles.Should().Contain("Enable Caching");
        titles.Should().Contain("Optimize Database Queries");
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
        metrics.Should().Contain("Throughput");
        metrics.Should().Contain("Response Time");
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
        result.CriticalIssues.Should().Contain(issue => issue.Contains("memory usage"));
        result.CriticalIssues.Should().Contain(issue => issue.Contains("order processing"));
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
            prediction.PredictedValue.Should().NotBeNullOrEmpty();
            prediction.PredictedValue.Should().Match(p => p.Contains("req/sec") || p.Contains("ms avg"));
        }
    }
}