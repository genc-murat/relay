using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Validation;

public class AIValidationFrameworkOptimizationResultsTests
{
    private readonly Mock<ILogger<AIValidationFramework>> _mockLogger;
    private readonly AIValidationFramework _validationFramework;
    private readonly AIOptimizationOptions _options;

    public AIValidationFrameworkOptimizationResultsTests()
    {
        _mockLogger = new Mock<ILogger<AIValidationFramework>>();
        _options = new AIOptimizationOptions
        {
            MinConfidenceScore = 0.7,
            MaxAutomaticOptimizationRisk = RiskLevel.Medium,
            EnableAutomaticOptimization = true
        };
        _validationFramework = new AIValidationFramework(_mockLogger.Object, _options);
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithSuccessfulOptimization_ReturnsSuccess()
    {
        // Arrange
        var beforeMetrics = CreateBaselineMetrics();
        var afterMetrics = CreateImprovedMetrics();
        var strategies = new[] { OptimizationStrategy.EnableCaching };

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.True(result.WasSuccessful);
        Assert.True(result.OverallImprovement > 0);
        Assert.Single(result.StrategyResults);
        Assert.True(result.StrategyResults[0].WasSuccessful);
        Assert.True(result.StrategyResults[0].ActualImprovement.TotalMilliseconds > 0);
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithNoImprovement_ReturnsFailure()
    {
        // Arrange
        var beforeMetrics = CreateBaselineMetrics();
        var afterMetrics = CreateBaselineMetrics(); // Same metrics - no improvement
        var strategies = new[] { OptimizationStrategy.EnableCaching };

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.False(result.WasSuccessful);
        Assert.True(result.OverallImprovement <= 0);
        Assert.Single(result.StrategyResults);
        Assert.False(result.StrategyResults[0].WasSuccessful);
        Assert.True(result.StrategyResults[0].ActualImprovement.TotalMilliseconds <= 0);
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithDegradation_ReturnsFailure()
    {
        // Arrange
        var beforeMetrics = CreateImprovedMetrics();
        var afterMetrics = CreateBaselineMetrics(); // Worse performance
        var strategies = new[] { OptimizationStrategy.EnableCaching };

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.False(result.WasSuccessful);
        Assert.True(result.OverallImprovement < 0);
        Assert.Single(result.StrategyResults);
        Assert.False(result.StrategyResults[0].WasSuccessful);
        Assert.True(result.StrategyResults[0].ActualImprovement.TotalMilliseconds < 0);
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithMultipleStrategies_ValidatesAll()
    {
        // Arrange
        var beforeMetrics = CreateBaselineMetrics();
        var afterMetrics = CreateImprovedMetrics();
        var strategies = new[]
        {
            OptimizationStrategy.EnableCaching,
            OptimizationStrategy.BatchProcessing,
            OptimizationStrategy.MemoryPooling
        };

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.True(result.WasSuccessful);
        Assert.True(result.OverallImprovement > 0);
        Assert.Equal(3, result.StrategyResults.Length);
        Assert.All(result.StrategyResults, sr => Assert.True(sr.WasSuccessful));
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithMixedResults_ReturnsPartialSuccess()
    {
        // Arrange
        var beforeMetrics = CreateBaselineMetrics();
        var afterMetrics = CreateSlightlyImprovedMetrics(); // Small improvement
        var strategies = new[]
        {
            OptimizationStrategy.EnableCaching,
            OptimizationStrategy.BatchProcessing
        };

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.True(result.WasSuccessful); // Overall improvement
        Assert.True(result.OverallImprovement > 0);
        Assert.Equal(2, result.StrategyResults.Length);
        // All strategies show the same improvement since they're applied to the same metrics
        Assert.All(result.StrategyResults, sr => Assert.True(sr.WasSuccessful));
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithSignificantTimeImprovement_CalculatesCorrectGain()
    {
        // Arrange
        var beforeMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(200),
            P95ExecutionTime = TimeSpan.FromMilliseconds(500),
            ConcurrentExecutions = 10,
            MemoryUsage = 100 * 1024 * 1024,
            DatabaseCalls = 50,
            ExternalApiCalls = 20,
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.95,
            MemoryAllocated = 100 * 1024 * 1024
        };

        var afterMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 980,
            FailedExecutions = 20,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100), // 50% improvement
            P95ExecutionTime = TimeSpan.FromMilliseconds(250), // 50% improvement
            ConcurrentExecutions = 10,
            MemoryUsage = 80 * 1024 * 1024, // 20% improvement
            DatabaseCalls = 30, // 40% improvement
            ExternalApiCalls = 10, // 50% improvement
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.98, // Improvement
            MemoryAllocated = 80 * 1024 * 1024 // 20% improvement
        };

        var strategies = new[] { OptimizationStrategy.EnableCaching };

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.True(result.WasSuccessful);
        Assert.True(result.OverallImprovement > 0);
        var strategyResult = result.StrategyResults[0];
        Assert.True(strategyResult.PerformanceGain > 0.4); // Should be around 50%
        Assert.Equal(TimeSpan.FromMilliseconds(100), strategyResult.ActualImprovement);
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithMemoryImprovement_CalculatesCorrectGain()
    {
        // Arrange
        var beforeMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P95ExecutionTime = TimeSpan.FromMilliseconds(200),
            ConcurrentExecutions = 10,
            MemoryUsage = 200 * 1024 * 1024,
            DatabaseCalls = 50,
            ExternalApiCalls = 20,
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.95,
            MemoryAllocated = 200 * 1024 * 1024
        };

        var afterMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P95ExecutionTime = TimeSpan.FromMilliseconds(200),
            ConcurrentExecutions = 10,
            MemoryUsage = 100 * 1024 * 1024, // 50% memory improvement
            DatabaseCalls = 50,
            ExternalApiCalls = 20,
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.95,
            MemoryAllocated = 100 * 1024 * 1024 // 50% memory improvement
        };

        var strategies = new[] { OptimizationStrategy.MemoryPooling };

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.True(result.WasSuccessful);
        Assert.True(result.OverallImprovement > 0);
        var strategyResult = result.StrategyResults[0];
        Assert.True(strategyResult.PerformanceGain > 0); // Should show improvement
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithSuccessRateImprovement_CalculatesCorrectGain()
    {
        // Arrange
        var beforeMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 900,
            FailedExecutions = 100,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P95ExecutionTime = TimeSpan.FromMilliseconds(200),
            ConcurrentExecutions = 10,
            MemoryUsage = 100 * 1024 * 1024,
            DatabaseCalls = 50,
            ExternalApiCalls = 20,
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.9,
            MemoryAllocated = 100 * 1024 * 1024
        };

        var afterMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 990,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P95ExecutionTime = TimeSpan.FromMilliseconds(200),
            ConcurrentExecutions = 10,
            MemoryUsage = 100 * 1024 * 1024,
            DatabaseCalls = 50,
            ExternalApiCalls = 20,
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.99, // Significant success rate improvement
            MemoryAllocated = 100 * 1024 * 1024
        };

        var strategies = new[] { OptimizationStrategy.EnableCaching };

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.True(result.WasSuccessful);
        Assert.True(result.OverallImprovement > 0);
        var strategyResult = result.StrategyResults[0];
        Assert.True(strategyResult.PerformanceGain > 0);
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithZeroExecutions_HandlesGracefully()
    {
        // Arrange
        var beforeMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 0,
            SuccessfulExecutions = 0,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.Zero,
            P95ExecutionTime = TimeSpan.Zero,
            ConcurrentExecutions = 0,
            MemoryUsage = 0,
            DatabaseCalls = 0,
            ExternalApiCalls = 0,
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0,
            MemoryAllocated = 0
        };

        var afterMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 0,
            SuccessfulExecutions = 0,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.Zero,
            P95ExecutionTime = TimeSpan.Zero,
            ConcurrentExecutions = 0,
            MemoryUsage = 0,
            DatabaseCalls = 0,
            ExternalApiCalls = 0,
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0,
            MemoryAllocated = 0
        };

        var strategies = new[] { OptimizationStrategy.EnableCaching };

        // Act & Assert - Should not throw
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Should handle gracefully
        Assert.False(result.WasSuccessful); // No improvement with zero metrics
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithNullStrategies_HandlesGracefully()
    {
        // Arrange
        var beforeMetrics = CreateBaselineMetrics();
        var afterMetrics = CreateImprovedMetrics();
        OptimizationStrategy[] strategies = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _validationFramework.ValidateOptimizationResultsAsync(
                strategies, beforeMetrics, afterMetrics).AsTask());
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithEmptyStrategies_ReturnsNoResults()
    {
        // Arrange
        var beforeMetrics = CreateBaselineMetrics();
        var afterMetrics = CreateImprovedMetrics();
        var strategies = Array.Empty<OptimizationStrategy>();

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.False(result.WasSuccessful); // No strategies applied
        Assert.Equal(0, result.OverallImprovement);
        Assert.Empty(result.StrategyResults);
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithNullBeforeMetrics_ThrowsArgumentNullException()
    {
        // Arrange
        RequestExecutionMetrics beforeMetrics = null!;
        var afterMetrics = CreateImprovedMetrics();
        var strategies = new[] { OptimizationStrategy.EnableCaching };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _validationFramework.ValidateOptimizationResultsAsync(
                strategies, beforeMetrics, afterMetrics).AsTask());
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithNullAfterMetrics_ThrowsArgumentNullException()
    {
        // Arrange
        var beforeMetrics = CreateBaselineMetrics();
        RequestExecutionMetrics afterMetrics = null!;
        var strategies = new[] { OptimizationStrategy.EnableCaching };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _validationFramework.ValidateOptimizationResultsAsync(
                strategies, beforeMetrics, afterMetrics).AsTask());
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var beforeMetrics = CreateBaselineMetrics();
        var afterMetrics = CreateImprovedMetrics();
        var strategies = new[] { OptimizationStrategy.EnableCaching };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _validationFramework.ValidateOptimizationResultsAsync(
                strategies, beforeMetrics, afterMetrics, cts.Token).AsTask());
    }

    [Fact]
    public async Task ValidateOptimizationResultsAsync_WithComplexScenario_CalculatesCorrectOverallImprovement()
    {
        // Arrange - Complex scenario with mixed improvements
        var beforeMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 900,
            FailedExecutions = 100,
            AverageExecutionTime = TimeSpan.FromMilliseconds(200),
            P95ExecutionTime = TimeSpan.FromMilliseconds(400),
            ConcurrentExecutions = 10,
            MemoryUsage = 200 * 1024 * 1024,
            DatabaseCalls = 100,
            ExternalApiCalls = 50,
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.9,
            MemoryAllocated = 200 * 1024 * 1024
        };

        var afterMetrics = new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 980,
            FailedExecutions = 20,
            AverageExecutionTime = TimeSpan.FromMilliseconds(120), // 40% time improvement
            P95ExecutionTime = TimeSpan.FromMilliseconds(240), // 40% time improvement
            ConcurrentExecutions = 10,
            MemoryUsage = 150 * 1024 * 1024, // 25% memory improvement
            DatabaseCalls = 60, // 40% database call improvement
            ExternalApiCalls = 25, // 50% API call improvement
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.98, // Success rate improvement
            MemoryAllocated = 150 * 1024 * 1024 // 25% memory improvement
        };

        var strategies = new[]
        {
            OptimizationStrategy.EnableCaching,
            OptimizationStrategy.BatchProcessing,
            OptimizationStrategy.MemoryPooling
        };

        // Act
        var result = await _validationFramework.ValidateOptimizationResultsAsync(
            strategies, beforeMetrics, afterMetrics);

        // Assert
        Assert.True(result.WasSuccessful);
        Assert.True(result.OverallImprovement > 0.3); // Should show significant improvement
        Assert.Equal(3, result.StrategyResults.Length);
        Assert.All(result.StrategyResults, sr => Assert.True(sr.WasSuccessful));
    }

    private RequestExecutionMetrics CreateBaselineMetrics()
    {
        return new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            P95ExecutionTime = TimeSpan.FromMilliseconds(200),
            ConcurrentExecutions = 10,
            MemoryUsage = 100 * 1024 * 1024,
            DatabaseCalls = 50,
            ExternalApiCalls = 20,
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.95,
            MemoryAllocated = 100 * 1024 * 1024
        };
    }

    private RequestExecutionMetrics CreateImprovedMetrics()
    {
        return new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 980,
            FailedExecutions = 20,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50), // 50% improvement
            P95ExecutionTime = TimeSpan.FromMilliseconds(100), // 50% improvement
            ConcurrentExecutions = 10,
            MemoryUsage = 80 * 1024 * 1024, // 20% improvement
            DatabaseCalls = 30, // 40% improvement
            ExternalApiCalls = 10, // 50% improvement
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.98, // Improvement
            MemoryAllocated = 80 * 1024 * 1024 // 20% improvement
        };
    }

    private RequestExecutionMetrics CreateSlightlyImprovedMetrics()
    {
        return new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 960,
            FailedExecutions = 40,
            AverageExecutionTime = TimeSpan.FromMilliseconds(90), // 10% improvement
            P95ExecutionTime = TimeSpan.FromMilliseconds(180), // 10% improvement
            ConcurrentExecutions = 10,
            MemoryUsage = 95 * 1024 * 1024, // 5% improvement
            DatabaseCalls = 45, // 10% improvement
            ExternalApiCalls = 18, // 10% improvement
            LastExecution = DateTime.UtcNow,
            SuccessRate = 0.96, // Small improvement
            MemoryAllocated = 95 * 1024 * 1024 // 5% improvement
        };
    }


}