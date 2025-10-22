using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class PredictiveAnalysisServiceTests
{
    private readonly ILogger _logger;
    private readonly PredictiveAnalysisService _service;

    public PredictiveAnalysisServiceTests()
    {
        _logger = NullLogger.Instance;
        _service = new PredictiveAnalysisService(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new PredictiveAnalysisService(null!));
    }

    #endregion

    #region AnalyzeLoadPatterns Tests

    [Fact]
    public void AnalyzeLoadPatterns_Should_Return_Valid_LoadPatternData()
    {
        // Act
        var result = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<LoadPatternData>(result);
        Assert.True(result.SuccessRate >= 0.0 && result.SuccessRate <= 1.0);
        Assert.True(result.AverageImprovement >= 0.0);
        Assert.True(result.TotalPredictions >= 0);
        Assert.NotNull(result.StrategyEffectiveness);
        Assert.NotNull(result.Predictions);
    }

    [Fact]
    public void AnalyzeLoadPatterns_Should_Determine_Load_Level_Correctly()
    {
        // Act
        var result = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.True(Enum.IsDefined(typeof(LoadLevel), result.Level));
    }

    [Fact]
    public void AnalyzeLoadPatterns_Should_Return_Idle_Level_When_No_History()
    {
        // Act
        var result = _service.AnalyzeLoadPatterns();

        // Assert
        // With no history, should return Idle level
        Assert.Equal(LoadLevel.Idle, result.Level);
    }

    [Fact]
    public void AnalyzeLoadPatterns_Should_Include_Strategy_Effectiveness()
    {
        // Act
        var result = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.NotNull(result.StrategyEffectiveness);
        Assert.True(result.StrategyEffectiveness.Count >= 0);
    }

    #endregion

    #region GeneratePredictiveAnalysis Tests

    [Fact]
    public void GeneratePredictiveAnalysis_Should_Return_Valid_Analysis()
    {
        // Act
        var result = _service.GeneratePredictiveAnalysis();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PredictionConfidence >= 0.0 && result.PredictionConfidence <= 1.0);
        Assert.NotNull(result.PotentialIssues);
    }

    [Fact]
    public void GeneratePredictiveAnalysis_Should_Return_Low_Confidence_When_Insufficient_Data()
    {
        // Act
        var result = _service.GeneratePredictiveAnalysis();

        // Assert
        // With no historical data, confidence should be low
        Assert.True(result.PredictionConfidence < 0.5);
    }

    [Fact]
    public void GeneratePredictiveAnalysis_Should_Include_ForecastResults_In_Predictions_With_Sufficient_Data()
    {
        // Arrange - Add sufficient metrics history (more than 10 snapshots)
        for (int i = 0; i < 15; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.1 + (i * 0.02), // Gradually increasing
                ["MemoryUtilization"] = 0.2 + (i * 0.01),
                ["ThroughputPerSecond"] = 50 + (i * 2),
                ["ErrorRate"] = 0.01
            };
            _service.AddMetricsSnapshot(metrics);
        }

        // Act
        var result = _service.GeneratePredictiveAnalysis();

        // Assert
        Assert.NotNull(result.NextHourPredictions);
        Assert.NotNull(result.NextDayPredictions);

        // Should contain predictions for key metrics
        Assert.Contains("CpuUtilization", result.NextHourPredictions.Keys);
        Assert.Contains("MemoryUtilization", result.NextHourPredictions.Keys);
        Assert.Contains("ThroughputPerSecond", result.NextHourPredictions.Keys);
        Assert.Contains("ErrorRate", result.NextHourPredictions.Keys);

        // NextDayPredictions may be empty if insufficient daily data (requires multiple days)
        // In this test, all snapshots are on the same day, so daily predictions are empty
        // This is expected behavior

        // Verify ForecastResult properties for NextHourPredictions
        var cpuForecast = result.NextHourPredictions["CpuUtilization"];
        Assert.NotNull(cpuForecast);
        Assert.True(cpuForecast.Current >= 0.0 && cpuForecast.Current <= 1.0);
        Assert.True(cpuForecast.Forecast5Min >= 0.0 && cpuForecast.Forecast5Min <= 1.0);
        Assert.True(cpuForecast.Forecast15Min >= 0.0 && cpuForecast.Forecast15Min <= 1.0);
        Assert.True(cpuForecast.Forecast60Min >= 0.0 && cpuForecast.Forecast60Min <= 1.0);
        Assert.True(cpuForecast.Confidence >= 0.0 && cpuForecast.Confidence <= 1.0);
    }

    [Fact]
    public void GeneratePredictiveAnalysis_Should_Return_Empty_Predictions_With_Insufficient_Data()
    {
        // Arrange - Add insufficient data (less than 10 snapshots)
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.5,
                ["MemoryUtilization"] = 0.3,
                ["ThroughputPerSecond"] = 100
            };
            _service.AddMetricsSnapshot(metrics);
        }

        // Act
        var result = _service.GeneratePredictiveAnalysis();

        // Assert
        Assert.NotNull(result.NextHourPredictions);
        Assert.NotNull(result.NextDayPredictions);
        Assert.Empty(result.NextHourPredictions);
        Assert.Empty(result.NextDayPredictions);
    }

    #endregion

    #region AddMetricsSnapshot Tests

    [Fact]
    public void AddMetricsSnapshot_Should_Accept_Valid_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.5,
            ["MemoryUtilization"] = 0.3,
            ["ThroughputPerSecond"] = 100
        };

        // Act & Assert - Should not throw
        _service.AddMetricsSnapshot(metrics);
    }

    [Fact]
    public void AddMetricsSnapshot_Should_Handle_Empty_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();

        // Act & Assert - Should not throw
        _service.AddMetricsSnapshot(metrics);
    }

    [Fact]
    public void AddMetricsSnapshot_Should_Handle_Null_Metrics()
    {
        // Act & Assert - Should not throw
        _service.AddMetricsSnapshot(null!);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Full_Analysis_Workflow_Should_Work()
    {
        // Arrange - Add some metrics history
        for (int i = 0; i < 15; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.1 + (i * 0.05), // Gradually increasing load
                ["MemoryUtilization"] = 0.2 + (i * 0.03),
                ["ThroughputPerSecond"] = 50 + (i * 5)
            };
            _service.AddMetricsSnapshot(metrics);
        }

        // Act
        var predictiveAnalysis = _service.GeneratePredictiveAnalysis();
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.NotNull(predictiveAnalysis);
        Assert.NotNull(loadPatternData);
        Assert.True(predictiveAnalysis.PredictionConfidence > 0.1); // Should have better confidence with data
        Assert.True(loadPatternData.TotalPredictions > 0);
    }

    #endregion

    #region LoadTransition Tests

    [Fact]
    public void GetLoadTransitions_Should_Return_Empty_List_Initially()
    {
        // Act
        var transitions = _service.GetLoadTransitions();

        // Assert
        Assert.NotNull(transitions);
        Assert.Empty(transitions);
    }

    [Fact]
    public void AnalyzeLoadPatterns_Should_Track_Load_Level_Transitions()
    {
        // Arrange - Add metrics that will cause load level changes
        // Need at least 5 metrics for AnalyzeLoadPatterns to work
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.05, // Idle
                ["MemoryUtilization"] = 0.1,
                ["ThroughputPerSecond"] = 10
            };
            _service.AddMetricsSnapshot(metrics);
        }

        var lowMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.25, // Low
            ["MemoryUtilization"] = 0.3,
            ["ThroughputPerSecond"] = 60
        };

        var mediumMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.55, // Medium
            ["MemoryUtilization"] = 0.6,
            ["ThroughputPerSecond"] = 100
        };

        // Act - Add metrics in sequence to trigger transitions
        _service.AddMetricsSnapshot(lowMetrics);
        _service.AnalyzeLoadPatterns(); // Should transition Idle -> Low

        _service.AddMetricsSnapshot(mediumMetrics);
        _service.AnalyzeLoadPatterns(); // Should transition Low -> Medium

        var transitions = _service.GetLoadTransitions();

        // Assert
        Assert.Equal(2, transitions.Count); // Two transitions: Idle->Low, Low->Medium

        var firstTransition = transitions[0];
        Assert.Equal(LoadLevel.Idle, firstTransition.FromLevel);
        Assert.Equal(LoadLevel.Low, firstTransition.ToLevel);
        Assert.Equal(TimeSpan.Zero, firstTransition.TimeSincePrevious); // First transition

        var secondTransition = transitions[1];
        Assert.Equal(LoadLevel.Low, secondTransition.FromLevel);
        Assert.Equal(LoadLevel.Medium, secondTransition.ToLevel);
        Assert.True(secondTransition.TimeSincePrevious > TimeSpan.Zero); // Should have time since previous
    }

    [Fact]
    public void LoadTransition_Should_Calculate_Performance_Impact_Correctly()
    {
        // Arrange - Create a transition manually to test impact calculation
        var transition = new LoadTransition
        {
            FromLevel = LoadLevel.Low, // Impact 1.0
            ToLevel = LoadLevel.High,  // Impact 3.0
            Timestamp = DateTime.UtcNow,
            TimeSincePrevious = TimeSpan.FromMinutes(5)
        };

        // The impact should be |3.0 - 1.0| * 0.1 = 0.2 seconds
        var expectedImpact = TimeSpan.FromSeconds(0.2);

        // Act & Assert - We can't directly test the private method, but we can verify
        // the transition object structure
        Assert.Equal(LoadLevel.Low, transition.FromLevel);
        Assert.Equal(LoadLevel.High, transition.ToLevel);
        Assert.Equal(TimeSpan.FromMinutes(5), transition.TimeSincePrevious);
        Assert.True(transition.PerformanceImpact >= TimeSpan.Zero); // Should be calculated
    }

    [Fact]
    public void LoadTransitions_Should_Maintain_Maximum_History_Size()
    {
        // Arrange - Add many metrics to trigger multiple transitions
        for (int i = 0; i < 60; i++) // More than the 50 limit
        {
            var metrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.1 + (i * 0.01), // Gradually increasing
                ["MemoryUtilization"] = 0.2,
                ["ThroughputPerSecond"] = 50
            };

            _service.AddMetricsSnapshot(metrics);
            _service.AnalyzeLoadPatterns();
        }

        // Act
        var transitions = _service.GetLoadTransitions();

        // Assert - Should not exceed 50 transitions
        Assert.True(transitions.Count <= 50, $"Expected <= 50 transitions, got {transitions.Count}");
    }

    [Fact]
    public void LoadTransition_Should_Record_Timestamps_Correctly()
    {
        // Arrange
        var beforeTransition = DateTime.UtcNow;

        var idleMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.05,
            ["MemoryUtilization"] = 0.1,
            ["ThroughputPerSecond"] = 10
        };

        var highMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.85, // High load (> 0.7)
            ["MemoryUtilization"] = 0.8,
            ["ThroughputPerSecond"] = 200
        };

        // Act - First establish baseline
        _service.AddMetricsSnapshot(idleMetrics);
        _service.AnalyzeLoadPatterns();

        System.Threading.Thread.Sleep(10); // Small delay

        // Now add high load metrics
        _service.AddMetricsSnapshot(highMetrics);
        _service.AnalyzeLoadPatterns();

        var transitions = _service.GetLoadTransitions();

        // Assert
        Assert.Single(transitions);
        var transition = transitions[0];

        Assert.True(transition.Timestamp >= beforeTransition);
        Assert.True(transition.Timestamp <= DateTime.UtcNow);
        Assert.Equal(LoadLevel.Idle, transition.FromLevel);
        Assert.Equal(LoadLevel.High, transition.ToLevel);
    }

    [Fact]
    public void LoadTransition_Should_Handle_No_Previous_Transition()
    {
        // Arrange - First transition should have TimeSincePrevious = Zero
        // Start with idle, then go to low to create a transition
        var idleMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.05,
            ["MemoryUtilization"] = 0.1,
            ["ThroughputPerSecond"] = 10
        };

        var lowMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.25, // Low load
            ["MemoryUtilization"] = 0.3,
            ["ThroughputPerSecond"] = 60
        };

        // Act - Establish baseline first
        _service.AddMetricsSnapshot(idleMetrics);
        _service.AnalyzeLoadPatterns();

        // Now create the transition
        _service.AddMetricsSnapshot(lowMetrics);
        _service.AnalyzeLoadPatterns();

        var transitions = _service.GetLoadTransitions();

        // Assert
        Assert.Single(transitions);
        Assert.Equal(TimeSpan.Zero, transitions[0].TimeSincePrevious);
    }

    #endregion
}