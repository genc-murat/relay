using Relay.CLI.Commands;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class AICodeOptimizerTests
{
    private readonly AICodeOptimizer _optimizer;

    public AICodeOptimizerTests()
    {
        _optimizer = new AICodeOptimizer();
    }

    [Fact]
    public async Task OptimizeAsync_WithValidParameters_ReturnsOptimizationResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<AIOptimizationResults>();
    }

    [Fact]
    public async Task OptimizeAsync_ReturnsExpectedAppliedOptimizationsCount()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        result.AppliedOptimizations.Should().NotBeNull();
        result.AppliedOptimizations.Should().HaveCount(2);
    }

    [Fact]
    public async Task OptimizeAsync_ReturnsExpectedOverallImprovement()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        result.OverallImprovement.Should().Be(0.35);
    }

    [Fact]
    public async Task OptimizeAsync_ReturnsValidOverallImprovementRange()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        result.OverallImprovement.Should().BeGreaterThanOrEqualTo(0);
        result.OverallImprovement.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task OptimizeAsync_ReturnsCachingOptimizationResult()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        var cachingResult = result.AppliedOptimizations.FirstOrDefault(o => o.Strategy == "Caching");
        cachingResult.Should().NotBeNull();
        cachingResult.Strategy.Should().Be("Caching");
        cachingResult.FilePath.Should().Be("Services/UserService.cs");
        cachingResult.Description.Should().Be("Added [DistributedCache] attribute");
        cachingResult.Success.Should().BeTrue();
        cachingResult.PerformanceGain.Should().Be(0.6);
    }

    [Fact]
    public async Task OptimizeAsync_ReturnsAsyncOptimizationResult()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        var asyncResult = result.AppliedOptimizations.FirstOrDefault(o => o.Strategy == "Async");
        asyncResult.Should().NotBeNull();
        asyncResult.Strategy.Should().Be("Async");
        asyncResult.FilePath.Should().Be("Services/OrderService.cs");
        asyncResult.Description.Should().Be("Converted Task to ValueTask");
        asyncResult.Success.Should().BeTrue();
        asyncResult.PerformanceGain.Should().Be(0.1);
    }

    [Fact]
    public async Task OptimizeAsync_AllOptimizationResultsAreSuccessful()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        result.AppliedOptimizations.Should().AllSatisfy(o => o.Success.Should().BeTrue());
    }

    [Fact]
    public async Task OptimizeAsync_AllOptimizationResultsHaveRequiredProperties()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        foreach (var optimization in result.AppliedOptimizations)
        {
            optimization.Strategy.Should().NotBeNullOrEmpty();
            optimization.FilePath.Should().NotBeNullOrEmpty();
            optimization.Description.Should().NotBeNullOrEmpty();
            optimization.PerformanceGain.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task OptimizeAsync_PerformanceGainsAreWithinValidRange()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        foreach (var optimization in result.AppliedOptimizations)
        {
            optimization.PerformanceGain.Should().BeGreaterThanOrEqualTo(0);
            optimization.PerformanceGain.Should().BeLessThanOrEqualTo(1);
        }
    }

    [Fact]
    public async Task OptimizeAsync_WithDifferentStrategies_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies1 = new[] { "caching", "async" };
        var strategies2 = new[] { "database", "memory" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result1 = await _optimizer.OptimizeAsync(path, strategies1, riskLevel, backup, dryRun, confidenceThreshold);
        var result2 = await _optimizer.OptimizeAsync(path, strategies2, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        // Currently implementation doesn't use strategies parameter, so results are the same
        result1.AppliedOptimizations.Should().HaveCount(result2.AppliedOptimizations.Length);
        result1.OverallImprovement.Should().Be(result2.OverallImprovement);
    }

    [Fact]
    public async Task OptimizeAsync_WithDifferentRiskLevels_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevels = new[] { "very-low", "low", "medium", "high" };
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act & Assert
        foreach (var riskLevel in riskLevels)
        {
            var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);
            result.Should().NotBeNull();
            // Currently implementation doesn't use riskLevel parameter, so results are the same
            result.AppliedOptimizations.Should().HaveCount(2);
            result.OverallImprovement.Should().Be(0.35);
        }
    }

    [Fact]
    public async Task OptimizeAsync_WithBackupFalse_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = false;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        result.Should().NotBeNull();
        // Currently implementation doesn't use backup parameter, so results are the same
        result.AppliedOptimizations.Should().HaveCount(2);
        result.OverallImprovement.Should().Be(0.35);
    }

    [Fact]
    public async Task OptimizeAsync_WithDryRunTrue_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = true;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        result.Should().NotBeNull();
        // Currently implementation doesn't use dryRun parameter, so results are the same
        result.AppliedOptimizations.Should().HaveCount(2);
        result.OverallImprovement.Should().Be(0.35);
    }

    [Fact]
    public async Task OptimizeAsync_WithDifferentConfidenceThresholds_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThresholds = new[] { 0.5, 0.7, 0.8, 0.9 };

        // Act & Assert
        foreach (var threshold in confidenceThresholds)
        {
            var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, threshold);
            result.Should().NotBeNull();
            // Currently implementation doesn't use confidenceThreshold parameter, so results are the same
            result.AppliedOptimizations.Should().HaveCount(2);
            result.OverallImprovement.Should().Be(0.35);
        }
    }

    [Fact]
    public async Task OptimizeAsync_CompletesWithinReasonableTime()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);
        stopwatch.Stop();

        // Assert
        // Should complete in less than 3 seconds (simulated delay is 2 seconds)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task OptimizeAsync_ReturnsNonEmptyAppliedOptimizations()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        result.AppliedOptimizations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OptimizeAsync_FilePathsAreValid()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        foreach (var optimization in result.AppliedOptimizations)
        {
            optimization.FilePath.Should().Contain(".cs");
            optimization.FilePath.Should().Contain("Services/");
        }
    }

    [Fact]
    public async Task OptimizeAsync_StrategiesAreValid()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = new[] { "caching", "async" };
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        var validStrategies = new[] { "Caching", "Async" };
        foreach (var optimization in result.AppliedOptimizations)
        {
            validStrategies.Should().Contain(optimization.Strategy);
        }
    }

    [Fact]
    public async Task OptimizeAsync_WithEmptyStrategiesArray_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        var strategies = Array.Empty<string>();
        var riskLevel = "low";
        var backup = true;
        var dryRun = false;
        var confidenceThreshold = 0.8;

        // Act
        var result = await _optimizer.OptimizeAsync(path, strategies, riskLevel, backup, dryRun, confidenceThreshold);

        // Assert
        result.Should().NotBeNull();
        // Currently implementation doesn't use strategies parameter, so results are the same
        result.AppliedOptimizations.Should().HaveCount(2);
        result.OverallImprovement.Should().Be(0.35);
    }
}