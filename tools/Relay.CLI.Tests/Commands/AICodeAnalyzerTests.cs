using Relay.CLI.Commands;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class AICodeAnalyzerTests
{
    private readonly AICodeAnalyzer _analyzer;

    public AICodeAnalyzerTests()
    {
        _analyzer = new AICodeAnalyzer();
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidPath_ReturnsAnalysisResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<AIAnalysisResults>();
    }

    [Fact]
    public async Task AnalyzeAsync_SetsProjectPathCorrectly()
    {
        // Arrange
        var expectedPath = @"C:\my\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(expectedPath, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.ProjectPath.Should().Be(expectedPath);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsExpectedFilesAnalyzedCount()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.FilesAnalyzed.Should().Be(42);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsExpectedHandlersFoundCount()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.HandlersFound.Should().Be(15);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsValidPerformanceScore()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.PerformanceScore.Should().BeGreaterThanOrEqualTo(0);
        result.PerformanceScore.Should().BeLessThanOrEqualTo(10);
        result.PerformanceScore.Should().Be(7.8);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsValidAIConfidence()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.AIConfidence.Should().BeGreaterThanOrEqualTo(0);
        result.AIConfidence.Should().BeLessThanOrEqualTo(1);
        result.AIConfidence.Should().Be(0.87);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsPerformanceIssues()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.PerformanceIssues.Should().NotBeNull();
        result.PerformanceIssues.Should().HaveCount(2);

        var firstIssue = result.PerformanceIssues[0];
        firstIssue.Severity.Should().Be("High");
        firstIssue.Description.Should().Be("Handler without caching for repeated queries");
        firstIssue.Location.Should().Be("UserService.GetUser");
        firstIssue.Impact.Should().Be("High");

        var secondIssue = result.PerformanceIssues[1];
        secondIssue.Severity.Should().Be("Medium");
        secondIssue.Description.Should().Be("Multiple database calls in single handler");
        secondIssue.Location.Should().Be("OrderService.ProcessOrder");
        secondIssue.Impact.Should().Be("Medium");
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsOptimizationOpportunities()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.OptimizationOpportunities.Should().NotBeNull();
        result.OptimizationOpportunities.Should().HaveCount(2);

        var firstOpportunity = result.OptimizationOpportunities[0];
        firstOpportunity.Strategy.Should().Be("Caching");
        firstOpportunity.Description.Should().Be("Enable distributed caching for user queries");
        firstOpportunity.ExpectedImprovement.Should().Be(0.6);
        firstOpportunity.Confidence.Should().Be(0.9);
        firstOpportunity.RiskLevel.Should().Be("Low");

        var secondOpportunity = result.OptimizationOpportunities[1];
        secondOpportunity.Strategy.Should().Be("Batching");
        secondOpportunity.Description.Should().Be("Batch database operations in order processing");
        secondOpportunity.ExpectedImprovement.Should().Be(0.3);
        secondOpportunity.Confidence.Should().Be(0.8);
        secondOpportunity.RiskLevel.Should().Be("Medium");
    }

    [Fact]
    public async Task AnalyzeAsync_WithDifferentDepths_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var depths = new[] { "basic", "standard", "deep", "comprehensive" };
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act & Assert
        foreach (var depth in depths)
        {
            var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);
            result.Should().NotBeNull();
            result.ProjectPath.Should().Be(path);
            result.FilesAnalyzed.Should().Be(42); // Currently hardcoded
        }
    }

    [Fact]
    public async Task AnalyzeAsync_WithIncludeMetricsFalse_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = false;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.Should().NotBeNull();
        // Currently implementation doesn't use the flags, so results are the same
        result.PerformanceScore.Should().Be(7.8);
    }

    [Fact]
    public async Task AnalyzeAsync_WithSuggestOptimizationsFalse_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = false;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.Should().NotBeNull();
        // Currently implementation doesn't use the flags, so results are the same
        result.OptimizationOpportunities.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnalyzeAsync_CompletesWithinReasonableTime()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);
        stopwatch.Stop();

        // Assert
        // Should complete in less than 2 seconds (simulated delay is 1 second)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsNonEmptyCollections()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        result.PerformanceIssues.Should().NotBeEmpty();
        result.OptimizationOpportunities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_PerformanceIssuesHaveRequiredProperties()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        foreach (var issue in result.PerformanceIssues)
        {
            issue.Severity.Should().NotBeNullOrEmpty();
            issue.Description.Should().NotBeNullOrEmpty();
            issue.Location.Should().NotBeNullOrEmpty();
            issue.Impact.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task AnalyzeAsync_OptimizationOpportunitiesHaveRequiredProperties()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        foreach (var opportunity in result.OptimizationOpportunities)
        {
            opportunity.Strategy.Should().NotBeNullOrEmpty();
            opportunity.Description.Should().NotBeNullOrEmpty();
            opportunity.ExpectedImprovement.Should().BeGreaterThan(0);
            opportunity.Confidence.Should().BeGreaterThan(0);
            opportunity.RiskLevel.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task AnalyzeAsync_ExpectedImprovementIsWithinValidRange()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        foreach (var opportunity in result.OptimizationOpportunities)
        {
            opportunity.ExpectedImprovement.Should().BeGreaterThanOrEqualTo(0);
            opportunity.ExpectedImprovement.Should().BeLessThanOrEqualTo(1);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_ConfidenceIsWithinValidRange()
    {
        // Arrange
        var path = @"C:\test\project";
        var depth = "standard";
        var includeMetrics = true;
        var suggestOptimizations = true;

        // Act
        var result = await _analyzer.AnalyzeAsync(path, depth, includeMetrics, suggestOptimizations);

        // Assert
        foreach (var opportunity in result.OptimizationOpportunities)
        {
            opportunity.Confidence.Should().BeGreaterThanOrEqualTo(0);
            opportunity.Confidence.Should().BeLessThanOrEqualTo(1);
        }
    }
}