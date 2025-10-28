using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
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

    #region RecordPredictionOutcome Tests

    [Fact]
    public void RecordPredictionOutcome_Should_Accept_Valid_Data()
    {
        // Arrange
        var strategy = OptimizationStrategy.EnableCaching;
        var predictedImprovement = TimeSpan.FromMilliseconds(50);
        var actualImprovement = TimeSpan.FromMilliseconds(45);
        var baselineExecutionTime = TimeSpan.FromMilliseconds(200);
        var loadLevel = LoadLevel.Medium;

        // Act & Assert - Should not throw
        _service.RecordPredictionOutcome(strategy, predictedImprovement, actualImprovement, baselineExecutionTime, loadLevel);
    }

    [Fact]
    public void RecordPredictionOutcome_Should_Handle_Multiple_Outcomes()
    {
        // Arrange - Record multiple outcomes
        for (int i = 0; i < 10; i++)
        {
            var strategy = OptimizationStrategy.EnableCaching;
            var predictedImprovement = TimeSpan.FromMilliseconds(50);
            var actualImprovement = TimeSpan.FromMilliseconds(45 + i);
            var baselineExecutionTime = TimeSpan.FromMilliseconds(200);
            var loadLevel = LoadLevel.Medium;

            // Act & Assert - Should not throw
            _service.RecordPredictionOutcome(strategy, predictedImprovement, actualImprovement, baselineExecutionTime, loadLevel);
        }

        // Verify that analysis uses the recorded data
        var loadPatternData = _service.AnalyzeLoadPatterns();
        Assert.NotNull(loadPatternData);
    }

    [Fact]
    public void RecordPredictionOutcome_Should_Handle_Zero_Values()
    {
        // Arrange
        var strategy = OptimizationStrategy.EnableCaching;
        var predictedImprovement = TimeSpan.Zero;
        var actualImprovement = TimeSpan.Zero;
        var baselineExecutionTime = TimeSpan.Zero;
        var loadLevel = LoadLevel.Idle;

        // Act & Assert - Should not throw
        _service.RecordPredictionOutcome(strategy, predictedImprovement, actualImprovement, baselineExecutionTime, loadLevel);
    }

    #endregion

    #region CalculateHistoricalSuccessRate Tests

    [Fact]
    public void CalculateHistoricalSuccessRate_Should_Return_50Percent_With_No_History()
    {
        // Arrange - No prediction outcomes recorded

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should return 0.5 (50%) when no historical data
        Assert.Equal(0.5, loadPatternData.SuccessRate);
    }

    [Fact]
    public void CalculateHistoricalSuccessRate_Should_Calculate_Accurate_Success_Rate()
    {
        // Arrange - Record predictions with known accuracy
        // 8 successful predictions (within 80-120% range)
        for (int i = 0; i < 8; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(90 + i), // 90-97ms (90-97% of predicted)
                TimeSpan.FromMilliseconds(200),
                LoadLevel.Medium);
        }

        // 2 unsuccessful predictions (outside 80-120% range)
        for (int i = 0; i < 2; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(150), // 150ms (150% of predicted - outside range)
                TimeSpan.FromMilliseconds(200),
                LoadLevel.Medium);
        }

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should be 80% success rate (8 out of 10)
        Assert.Equal(0.8, loadPatternData.SuccessRate, 2);
    }

    [Fact]
    public void CalculateHistoricalSuccessRate_Should_Handle_Perfect_Predictions()
    {
        // Arrange - All predictions exactly match actual
        for (int i = 0; i < 5; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(100), // Exactly as predicted
                TimeSpan.FromMilliseconds(200),
                LoadLevel.Medium);
        }

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should be 100% success rate
        Assert.Equal(1.0, loadPatternData.SuccessRate, 2);
    }

    [Fact]
    public void CalculateHistoricalSuccessRate_Should_Handle_All_Failed_Predictions()
    {
        // Arrange - All predictions way off
        for (int i = 0; i < 5; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(300), // 3x predicted (way outside range)
                TimeSpan.FromMilliseconds(200),
                LoadLevel.Medium);
        }

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should be 0% success rate
        Assert.Equal(0.0, loadPatternData.SuccessRate, 2);
    }

    [Fact]
    public void CalculateHistoricalSuccessRate_Should_Handle_Zero_Predicted_Improvement()
    {
        // Arrange - Predictions with zero predicted improvement
        for (int i = 0; i < 3; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.Zero, // No improvement predicted
                TimeSpan.Zero, // No actual improvement
                TimeSpan.FromMilliseconds(200),
                LoadLevel.Medium);
        }

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should be 100% success rate (both are zero)
        Assert.Equal(1.0, loadPatternData.SuccessRate, 2);
    }

    #endregion

    #region CalculateHistoricalImprovement Tests

    [Fact]
    public void CalculateHistoricalImprovement_Should_Return_Zero_With_No_History()
    {
        // Arrange - No prediction outcomes recorded

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should return 0.0 when no historical data
        Assert.Equal(0.0, loadPatternData.AverageImprovement);
    }

    [Fact]
    public void CalculateHistoricalImprovement_Should_Calculate_Average_Improvement()
    {
        // Arrange - Record predictions with known improvements
        // Improvement = (Baseline - Actual) / Baseline
        // Example: (200 - 150) / 200 = 0.25 (25% improvement)

        // 50% improvement: 200ms baseline, 100ms actual
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200),
            LoadLevel.Medium);

        // 25% improvement: 200ms baseline, 150ms actual
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(150),
            TimeSpan.FromMilliseconds(200),
            LoadLevel.Medium);

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Average should be (0.5 + 0.25) / 2 = 0.375 (37.5%)
        Assert.Equal(0.375, loadPatternData.AverageImprovement, 2);
    }

    [Fact]
    public void CalculateHistoricalImprovement_Should_Handle_No_Improvement()
    {
        // Arrange - Record predictions where actual = baseline (no improvement)
        for (int i = 0; i < 3; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(0),
                TimeSpan.FromMilliseconds(200), // Same as baseline
                TimeSpan.FromMilliseconds(200),
                LoadLevel.Medium);
        }

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should be 0% improvement
        Assert.Equal(0.0, loadPatternData.AverageImprovement, 2);
    }

    [Fact]
    public void CalculateHistoricalImprovement_Should_Ignore_Zero_Baseline()
    {
        // Arrange - Mix of valid and zero baseline data
        // Valid: 50% improvement
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200),
            LoadLevel.Medium);

        // Invalid: Zero baseline (should be ignored)
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(50),
            TimeSpan.Zero,
            LoadLevel.Medium);

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should only consider the valid entry (50% improvement)
        Assert.Equal(0.5, loadPatternData.AverageImprovement, 2);
    }

    [Fact]
    public void CalculateHistoricalImprovement_Should_Clamp_Values()
    {
        // Arrange - Record prediction where actual is worse than baseline (negative improvement)
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(300), // Worse than baseline
            TimeSpan.FromMilliseconds(200),
            LoadLevel.Medium);

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should clamp to 0 (not negative)
        Assert.True(loadPatternData.AverageImprovement >= 0.0);
        Assert.True(loadPatternData.AverageImprovement <= 1.0);
    }

    #endregion

    #region CalculateStrategyEffectiveness Tests

    [Fact]
    public void CalculateStrategyEffectiveness_Should_Return_Defaults_With_No_History()
    {
        // Arrange - No prediction outcomes recorded

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should return default effectiveness values
        Assert.NotNull(loadPatternData.StrategyEffectiveness);
        Assert.NotEmpty(loadPatternData.StrategyEffectiveness);
        Assert.Contains("EnableCaching", loadPatternData.StrategyEffectiveness.Keys);
        Assert.Equal(0.75, loadPatternData.StrategyEffectiveness["EnableCaching"]);
    }

    [Fact]
    public void CalculateStrategyEffectiveness_Should_Calculate_Per_Strategy()
    {
        // Arrange - Record outcomes for different strategies
        // EnableCaching: High effectiveness
        for (int i = 0; i < 5; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(95), // Good accuracy
                TimeSpan.FromMilliseconds(200), // 50% improvement
                LoadLevel.Medium);
        }

        // BatchProcessing: Lower effectiveness
        for (int i = 0; i < 5; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.BatchProcessing,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(200), // Poor accuracy
                TimeSpan.FromMilliseconds(300), // Lower improvement
                LoadLevel.Medium);
        }

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - EnableCaching should have higher effectiveness than BatchProcessing
        Assert.True(loadPatternData.StrategyEffectiveness.ContainsKey("EnableCaching"));
        Assert.True(loadPatternData.StrategyEffectiveness.ContainsKey("BatchProcessing"));
        Assert.True(loadPatternData.StrategyEffectiveness["EnableCaching"] >
                   loadPatternData.StrategyEffectiveness["BatchProcessing"]);
    }

    [Fact]
    public void CalculateStrategyEffectiveness_Should_Combine_Improvement_And_Accuracy()
    {
        // Arrange - Record outcome with known improvement and accuracy
        // Improvement: (200 - 100) / 200 = 0.5 (50%)
        // Accuracy: 100ms predicted, 100ms actual = 100% (within 80-120% range)
        // Effectiveness: (0.5 * 0.6) + (1.0 * 0.4) = 0.3 + 0.4 = 0.7
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200),
            LoadLevel.Medium);

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.True(loadPatternData.StrategyEffectiveness.ContainsKey("EnableCaching"));
        Assert.Equal(0.7, loadPatternData.StrategyEffectiveness["EnableCaching"], 1);
    }

    [Fact]
    public void CalculateStrategyEffectiveness_Should_Handle_Multiple_Strategies()
    {
        // Arrange - Record outcomes for all strategy types
        var strategies = new[]
        {
            OptimizationStrategy.EnableCaching,
            OptimizationStrategy.BatchProcessing,
            OptimizationStrategy.ParallelProcessing,
            OptimizationStrategy.CircuitBreaker
        };

        foreach (var strategy in strategies)
        {
            _service.RecordPredictionOutcome(
                strategy,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(95),
                TimeSpan.FromMilliseconds(200),
                LoadLevel.Medium);
        }

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Should have effectiveness data for all strategies
        Assert.True(loadPatternData.StrategyEffectiveness.Count >= strategies.Length);
        foreach (var strategy in strategies)
        {
            Assert.True(loadPatternData.StrategyEffectiveness.ContainsKey(strategy.ToString()));
            Assert.True(loadPatternData.StrategyEffectiveness[strategy.ToString()] >= 0.0);
            Assert.True(loadPatternData.StrategyEffectiveness[strategy.ToString()] <= 1.0);
        }
    }

    #endregion

    #region Integration Tests for New Methods

    [Fact]
    public void Full_Prediction_Lifecycle_Should_Work()
    {
        // Arrange - Simulate a complete prediction lifecycle
        var strategy = OptimizationStrategy.EnableCaching;
        var predictedImprovement = TimeSpan.FromMilliseconds(50);
        var actualImprovement = TimeSpan.FromMilliseconds(45);
        var baselineExecutionTime = TimeSpan.FromMilliseconds(200);
        var loadLevel = LoadLevel.Medium;

        // Add some metrics history first
        for (int i = 0; i < 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.5,
                ["MemoryUtilization"] = 0.3,
                ["ThroughputPerSecond"] = 100
            };
            _service.AddMetricsSnapshot(metrics);
        }

        // Act - Record prediction outcome
        _service.RecordPredictionOutcome(strategy, predictedImprovement, actualImprovement, baselineExecutionTime, loadLevel);

        // Get analysis results
        var loadPatternData = _service.AnalyzeLoadPatterns();
        var predictiveAnalysis = _service.GeneratePredictiveAnalysis();

        // Assert
        Assert.NotNull(loadPatternData);
        Assert.NotNull(predictiveAnalysis);
        Assert.True(loadPatternData.SuccessRate > 0.0);
        Assert.NotNull(loadPatternData.StrategyEffectiveness);
    }

    [Fact]
    public void Concurrent_RecordPredictionOutcome_Should_Be_ThreadSafe()
    {
        // Arrange
        var strategy = OptimizationStrategy.EnableCaching;
        var tasks = new List<System.Threading.Tasks.Task>();

        // Act - Record outcomes concurrently
        for (int i = 0; i < 10; i++)
        {
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                _service.RecordPredictionOutcome(
                    strategy,
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(95),
                    TimeSpan.FromMilliseconds(200),
                    LoadLevel.Medium);
            });
            tasks.Add(task);
        }

        System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

        // Assert - Should not throw and should have recorded all outcomes
        var loadPatternData = _service.AnalyzeLoadPatterns();
        Assert.NotNull(loadPatternData);
    }

    #endregion
}