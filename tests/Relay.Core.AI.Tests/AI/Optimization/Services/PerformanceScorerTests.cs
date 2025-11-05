using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class PerformanceScorerTests
{
    private readonly ILogger _logger;
    private readonly PerformanceScorer _scorer;

    public PerformanceScorerTests()
    {
        _logger = NullLogger.Instance;
        _scorer = new PerformanceScorer(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new PerformanceScorer(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var scorer = new PerformanceScorer(logger);

        // Assert
        Assert.NotNull(scorer);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Name_Should_Return_Correct_Value()
    {
        // Act
        var name = _scorer.Name;

        // Assert
        Assert.Equal("PerformanceScorer", name);
    }

    #endregion

    #region CalculateScoreCore Tests

    [Fact]
    public void CalculateScore_Should_Return_Maximum_Score_With_Perfect_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.0, // No CPU usage
            ["MemoryUtilization"] = 0.0, // No memory usage
            ["ThroughputPerSecond"] = 1000 // Maximum throughput
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: cpuScore = 1.0, memoryScore = 1.0, throughputScore = 1.0 (1000/1000)
        // Final score = (1.0 + 1.0 + 1.0) / 3.0 = 1.0
        var expectedScore = 1.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Return_Minimum_Score_With_Worst_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 1.0, // Maximum CPU usage
            ["MemoryUtilization"] = 1.0, // Maximum memory usage
            ["ThroughputPerSecond"] = 0 // No throughput
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: cpuScore = 0.0, memoryScore = 0.0, throughputScore = 0.0
        // Final score = (0.0 + 0.0 + 0.0) / 3.0 = 0.0
        var expectedScore = 0.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Calculate_Correctly_With_High_CPU_Utilization()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.8, // High CPU usage
            ["MemoryUtilization"] = 0.0, // No memory usage
            ["ThroughputPerSecond"] = 1000 // Max throughput
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: cpuScore = 1.0 - 0.8 = 0.2
        // memoryScore = 1.0 - 0.0 = 1.0
        // throughputScore = min(1000/1000, 1.0) = 1.0
        // Final score = (0.2 + 1.0 + 1.0) / 3.0 = 0.733333
        var expectedScore = (0.2 + 1.0 + 1.0) / 3.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Calculate_Correctly_With_Low_Throughput()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.0, // No CPU usage
            ["MemoryUtilization"] = 0.0, // No memory usage
            ["ThroughputPerSecond"] = 10 // Low throughput
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: cpuScore = 1.0, memoryScore = 1.0
        // throughputScore = min(10/1000, 1.0) = 0.01
        // Final score = (1.0 + 1.0 + 0.01) / 3.0 = 0.67
        var expectedScore = (1.0 + 1.0 + 0.01) / 3.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Use_Default_Values_For_Missing_Metrics()
    {
        // Arrange - Only provide some metrics, others should use defaults
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.5, // Only provide CPU
            // MemoryUtilization and ThroughputPerSecond will use defaults (0.0 and 100 respectively)
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: cpuScore = 1.0 - 0.5 = 0.5
        // memoryScore = 1.0 - 0.0 = 1.0 (default memory)
        // throughputScore = min(100/1000, 1.0) = 0.1 (default throughput)
        // Final score = (0.5 + 1.0 + 0.1) / 3.0 = 0.533333
        var expectedScore = (0.5 + 1.0 + 0.1) / 3.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Normalize_Throughput_To_Max_1000()
    {
        // Arrange - Test with throughput higher than 1000
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.0,
            ["MemoryUtilization"] = 0.0,
            ["ThroughputPerSecond"] = 1500 // Above max normalization
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: cpuScore = 1.0, memoryScore = 1.0
        // throughputScore = min(1500/1000, 1.0) = 1.0 (clamped)
        // Final score = (1.0 + 1.0 + 1.0) / 3.0 = 1.0
        var expectedScore = 1.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Not_Exceed_1_0_Or_Fall_Below_0_0()
    {
        // Arrange - Test with extreme values
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 2.0, // Above 1.0
            ["MemoryUtilization"] = -0.5, // Below 0.0
            ["ThroughputPerSecond"] = -100 // Negative
        };

        // Act
        var score = _scorer.CalculateScore(metrics);

        // Assert
        Assert.True(score >= 0.0);
        Assert.True(score <= 1.0);

        // The metrics will be processed:
        // cpuScore = 1.0 - 2.0 = -1.0, but the calculation uses the raw value
        // Actually, it will use the raw value 2.0: 1.0 - 2.0 = -1.0, but then the normalization might affect
        // Let's compute manually: cpuScore = max(0, 1.0 - 2.0) = 0, no, that's not in the code
        // The code simply does: cpuScore = 1.0 - cpuUtil, so with 2.0 we get -1.0
        // But the implementation doesn't clamp scores individually before averaging
        // Actually, looking again: 1.0 - 2.0 = -1.0, so cpuScore = -1.0
        // memoryScore = 1.0 - (-0.5) = 1.5
        // throughputScore = max(0, min(-100/1000, 1.0)) = max(0, -0.1) = 0
        // Average = (-1.0 + 1.5 + 0) / 3.0 = 0.5/3.0 = 0.166666667
    }

    #endregion

    #region GetCriticalAreas Tests

    [Fact]
    public void GetCriticalAreas_Should_Return_Empty_When_No_Critical_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.8, // Below critical threshold (0.9)
            ["MemoryUtilization"] = 0.8 // Below critical threshold (0.9)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Empty(criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Return_Area_For_High_CPU_Utilization()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.95, // Above critical threshold (0.9)
            ["MemoryUtilization"] = 0.5
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High CPU utilization", criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Return_Area_For_High_Memory_Utilization()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.5,
            ["MemoryUtilization"] = 0.95 // Above critical threshold (0.9)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High memory utilization", criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Return_Multiple_Areas_For_Multiple_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.95, // Above critical threshold (0.9)
            ["MemoryUtilization"] = 0.95 // Above critical threshold (0.9)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Equal(2, criticalAreas.Count);
        Assert.Contains("High CPU utilization", criticalAreas);
        Assert.Contains("High memory utilization", criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Use_Default_For_Missing_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>(); // Empty - will use defaults

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Empty(criticalAreas); // Default values (0.0 for CPU and memory) are below threshold
    }

    #endregion

    #region GetRecommendations Tests

    [Fact]
    public void GetRecommendations_Should_Return_Empty_When_No_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.7, // Below recommendation threshold (0.8)
            ["MemoryUtilization"] = 0.7 // Below recommendation threshold (0.8)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.Empty(recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Return_Recommendation_For_High_CPU_Utilization()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.85, // Above recommendation threshold (0.8)
            ["MemoryUtilization"] = 0.5
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations);
        Assert.Contains("Consider optimizing CPU-intensive operations", recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Return_Recommendation_For_High_Memory_Utilization()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.5,
            ["MemoryUtilization"] = 0.85 // Above recommendation threshold (0.8)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations);
        Assert.Contains("Consider memory optimization techniques", recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Return_Multiple_Recommendations_For_Multiple_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.85, // Above recommendation threshold (0.8)
            ["MemoryUtilization"] = 0.85 // Above recommendation threshold (0.8)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.Equal(2, recommendations.Count);
        Assert.Contains("Consider optimizing CPU-intensive operations", recommendations);
        Assert.Contains("Consider memory optimization techniques", recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Use_Default_For_Missing_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>(); // Empty - will use defaults

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.Empty(recommendations); // Default values (0.0 for CPU and memory) are below threshold
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void CalculateScore_Should_Handle_NaN_And_Infinity_Gracefully()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = double.NaN,
            ["MemoryUtilization"] = double.PositiveInfinity,
            ["ThroughputPerSecond"] = double.NegativeInfinity
        };

        // Act & Assert - Should not throw
        var score = _scorer.CalculateScore(metrics);
        Assert.True(score >= 0.0);
        Assert.True(score <= 1.0);
    }

    [Fact]
    public void CalculateScore_Should_Handle_Very_High_Throughput()
    {
        // Arrange - Test with very high throughput
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.1,
            ["MemoryUtilization"] = 0.1,
            ["ThroughputPerSecond"] = 1000000 // Very high throughput
        };

        // Act
        var score = _scorer.CalculateScore(metrics);

        // Assert: Throughput should be normalized to maximum of 1.0
        Assert.True(score >= 0.0);
        Assert.True(score <= 1.0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Full_Scoring_Workflow_Should_Work_Together()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.85, // Above recommendation but below critical
            ["MemoryUtilization"] = 0.85, // Above recommendation but below critical
            ["ThroughputPerSecond"] = 500
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Values are not high enough for critical areas
        Assert.Empty(criticalAreas);
        
        // But they are high enough for recommendations
        Assert.Equal(2, recommendations.Count);
        Assert.Contains("Consider optimizing CPU-intensive operations", recommendations);
        Assert.Contains("Consider memory optimization techniques", recommendations);
    }

    [Fact]
    public void Scoring_With_Critical_Values_Should_Trigger_Both_Areas_And_Recommendations()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.95, // Above both thresholds (0.8 for recommendations, 0.9 for critical)
            ["MemoryUtilization"] = 0.95, // Above both thresholds (0.8 for recommendations, 0.9 for critical)
            ["ThroughputPerSecond"] = 200
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Values are high enough for critical areas
        Assert.Equal(2, criticalAreas.Count);
        Assert.Contains("High CPU utilization", criticalAreas);
        Assert.Contains("High memory utilization", criticalAreas);
        
        // And also trigger recommendations
        Assert.Equal(2, recommendations.Count);
        Assert.Contains("Consider optimizing CPU-intensive operations", recommendations);
        Assert.Contains("Consider memory optimization techniques", recommendations);
    }

    #endregion
}
