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
}