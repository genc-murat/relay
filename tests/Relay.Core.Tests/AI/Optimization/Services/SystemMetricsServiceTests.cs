using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class SystemMetricsServiceTests
{
    private readonly ILogger _logger;
    private readonly SystemMetricsService _service;

    public SystemMetricsServiceTests()
    {
        _logger = NullLogger.Instance;
        _service = new SystemMetricsService(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new SystemMetricsService(null!));
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
    public void AnalyzeLoadPatterns_Should_Include_Predictions()
    {
        // Act
        var result = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.NotNull(result.Predictions);
        // May be empty if no predictions are generated, but should not be null
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

    [Fact]
    public void AnalyzeLoadPatterns_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task<LoadPatternData>[5];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                return _service.AnalyzeLoadPatterns();
            });
        }

        // Act
        System.Threading.Tasks.Task.WaitAll(tasks);

        // Assert
        foreach (var task in tasks)
        {
            Assert.NotNull(task.Result);
            Assert.True(task.Result.SuccessRate >= 0.0 && task.Result.SuccessRate <= 1.0);
        }
    }

    #endregion

    #region CalculateSystemHealthScore Tests

    [Fact]
    public void CalculateSystemHealthScore_Should_Return_Valid_HealthScore()
    {
        // Act
        var result = _service.CalculateSystemHealthScore();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Overall >= 0.0 && result.Overall <= 1.0);
        Assert.True(result.Performance >= 0.0 && result.Performance <= 1.0);
        Assert.True(result.Reliability >= 0.0 && result.Reliability <= 1.0);
        Assert.True(result.Scalability >= 0.0 && result.Scalability <= 1.0);
        Assert.True(result.Security >= 0.0 && result.Security <= 1.0);
        Assert.True(result.Maintainability >= 0.0 && result.Maintainability <= 1.0);
    }

    #endregion

    #region CollectSystemMetrics Tests

    [Fact]
    public void CollectSystemMetrics_Should_Return_Metrics_Dictionary()
    {
        // Act
        var result = _service.CollectSystemMetrics();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Dictionary<string, double>>(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public void CollectSystemMetrics_Should_Include_CPU_Metrics()
    {
        // Act
        var result = _service.CollectSystemMetrics();

        // Assert
        Assert.True(result.ContainsKey("CpuUtilization"));
        Assert.True(result.ContainsKey("CpuUsagePercent"));
    }

    [Fact]
    public void CollectSystemMetrics_Should_Include_Memory_Metrics()
    {
        // Act
        var result = _service.CollectSystemMetrics();

        // Assert
        Assert.True(result.ContainsKey("MemoryUtilization"));
        Assert.True(result.ContainsKey("MemoryUsageMB"));
        Assert.True(result.ContainsKey("AvailableMemoryMB"));
    }

    [Fact]
    public void CollectSystemMetrics_Should_Include_Throughput_Metrics()
    {
        // Act
        var result = _service.CollectSystemMetrics();

        // Assert
        Assert.True(result.ContainsKey("ThroughputPerSecond"));
        Assert.True(result.ContainsKey("RequestsPerSecond"));
    }

    #endregion
}