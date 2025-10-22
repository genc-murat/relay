using Relay.CLI.Commands;

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
        Assert.NotNull(result);
        Assert.IsType<AIOptimizationResults>(result);
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
        Assert.NotNull(result.AppliedOptimizations);
        Assert.Equal(2, result.AppliedOptimizations.Length);
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
        Assert.Equal(0.35, result.OverallImprovement);
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
        Assert.True(result.OverallImprovement >= 0);
        Assert.True(result.OverallImprovement <= 1);
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
        Assert.NotNull(cachingResult);
        Assert.Equal("Caching", cachingResult.Strategy);
        Assert.Equal("Services/UserService.cs", cachingResult.FilePath);
        Assert.Equal("Added [DistributedCache] attribute", cachingResult.Description);
        Assert.True(cachingResult.Success);
        Assert.Equal(0.6, cachingResult.PerformanceGain);
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
        Assert.NotNull(asyncResult);
        Assert.Equal("Async", asyncResult.Strategy);
        Assert.Equal("Services/OrderService.cs", asyncResult.FilePath);
        Assert.Equal("Converted Task to ValueTask", asyncResult.Description);
        Assert.True(asyncResult.Success);
        Assert.Equal(0.1, asyncResult.PerformanceGain);
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
        foreach (var optimization in result.AppliedOptimizations)
        {
            Assert.True(optimization.Success);
        }
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
            Assert.False(string.IsNullOrWhiteSpace(optimization.Strategy));
            Assert.False(string.IsNullOrWhiteSpace(optimization.FilePath));
            Assert.False(string.IsNullOrWhiteSpace(optimization.Description));
            Assert.True(optimization.PerformanceGain >= 0);
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
            Assert.True(optimization.PerformanceGain >= 0);
            Assert.True(optimization.PerformanceGain <= 1);
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
        Assert.Equal(result2.AppliedOptimizations.Length, result1.AppliedOptimizations.Length);
        Assert.Equal(result2.OverallImprovement, result1.OverallImprovement);
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
            Assert.NotNull(result);
            // Currently implementation doesn't use riskLevel parameter, so results are the same
            Assert.Equal(2, result.AppliedOptimizations.Length);
            Assert.Equal(0.35, result.OverallImprovement);
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
        Assert.NotNull(result);
        // Currently implementation doesn't use backup parameter, so results are the same
        Assert.Equal(2, result.AppliedOptimizations.Length);
        Assert.Equal(0.35, result.OverallImprovement);
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
        Assert.NotNull(result);
        // Currently implementation doesn't use dryRun parameter, so results are the same
        Assert.Equal(2, result.AppliedOptimizations.Length);
        Assert.Equal(0.35, result.OverallImprovement);
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
            Assert.NotNull(result);
            // Currently implementation doesn't use confidenceThreshold parameter, so results are the same
            Assert.Equal(2, result.AppliedOptimizations.Length);
            Assert.Equal(0.35, result.OverallImprovement);
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
        Assert.True(stopwatch.ElapsedMilliseconds < 3000);
        Assert.NotNull(result);
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
        Assert.NotEmpty(result.AppliedOptimizations);
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
            Assert.Contains(".cs", optimization.FilePath);
            Assert.Contains("Services/", optimization.FilePath);
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
            Assert.Contains(optimization.Strategy, validStrategies);
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
        Assert.NotNull(result);
        // Currently implementation doesn't use strategies parameter, so results are the same
        Assert.Equal(2, result.AppliedOptimizations.Length);
        Assert.Equal(0.35, result.OverallImprovement);
    }
}
