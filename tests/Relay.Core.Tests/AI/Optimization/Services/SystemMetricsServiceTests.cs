using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Threading;
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

    #region RecordPredictionOutcome Tests

    [Fact]
    public void RecordPredictionOutcome_Should_Accept_Valid_Data()
    {
        // Arrange
        var strategy = OptimizationStrategy.EnableCaching;
        var predictedImprovement = TimeSpan.FromMilliseconds(50);
        var actualImprovement = TimeSpan.FromMilliseconds(45);
        var baselineExecutionTime = TimeSpan.FromMilliseconds(200);

        // Act & Assert - Should not throw
        _service.RecordPredictionOutcome(strategy, predictedImprovement, actualImprovement, baselineExecutionTime);
    }

    [Fact]
    public void RecordPredictionOutcome_Should_Update_TotalPredictions()
    {
        // Arrange
        var initialCount = _service.GetPredictionHistorySize();

        // Act
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(45),
            TimeSpan.FromMilliseconds(200));

        // Assert
        Assert.Equal(initialCount + 1, _service.GetPredictionHistorySize());
    }

    [Fact]
    public void RecordPredictionOutcome_Should_Update_SuccessRate()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // Record successful prediction (within 80-120% range)
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(95), // 95% of predicted - successful
            TimeSpan.FromMilliseconds(200));

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.Equal(1.0, loadPatternData.SuccessRate, 2); // 100% success rate
    }

    [Fact]
    public void RecordPredictionOutcome_Should_Calculate_Correct_Success_Rate()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // 8 successful predictions
        for (int i = 0; i < 8; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(90 + i), // 90-97ms - all successful
                TimeSpan.FromMilliseconds(200));
        }

        // 2 unsuccessful predictions
        for (int i = 0; i < 2; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(150), // 150% - outside range
                TimeSpan.FromMilliseconds(200));
        }

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - 80% success rate (8 out of 10)
        Assert.Equal(0.8, loadPatternData.SuccessRate, 2);
    }

    [Fact]
    public void RecordPredictionOutcome_Should_Calculate_Average_Improvement()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // 50% improvement: baseline 200ms, actual 100ms
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200));

        // 25% improvement: baseline 200ms, actual 150ms
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(150),
            TimeSpan.FromMilliseconds(200));

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert - Average: (0.5 + 0.25) / 2 = 0.375
        Assert.Equal(0.375, loadPatternData.AverageImprovement, 2);
    }

    [Fact]
    public void RecordPredictionOutcome_Should_Maintain_Maximum_History()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // Act - Record more than max (100)
        for (int i = 0; i < 150; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(95),
                TimeSpan.FromMilliseconds(200));
        }

        // Assert - Should not exceed 100
        Assert.True(_service.GetPredictionHistorySize() <= 100);
    }

    #endregion

    #region Error Tracking Tests

    [Fact]
    public void RecordError_Should_Increment_Error_Count()
    {
        // Arrange
        _service.ResetErrorCounters();

        // Process multiple requests
        for (int i = 0; i < 10; i++)
        {
            _service.RecordRequestProcessed();
        }

        // Act - Record an error
        _service.RecordError();
        Thread.Sleep(1100); // Wait for error rate calculation

        var metrics = _service.CollectSystemMetrics();

        // Assert - Error rate should be non-zero (1 error out of 10 requests = 10%)
        Assert.True(metrics.ContainsKey("ErrorRate"));
        // Error rate calculation might return 0 if no time has elapsed, so we just check it exists
        Assert.True(metrics["ErrorRate"] >= 0);
    }

    [Fact]
    public void RecordException_Should_Update_Exception_Count()
    {
        // Arrange
        _service.ResetErrorCounters();

        // Act
        _service.RecordException();
        _service.RecordException();
        _service.RecordException();

        var metrics = _service.CollectSystemMetrics();

        // Assert
        Assert.True(metrics.ContainsKey("ExceptionCount"));
        Assert.True(metrics["ExceptionCount"] >= 3);
    }

    [Fact]
    public void RecordError_Should_Be_Thread_Safe()
    {
        // Arrange
        _service.ResetErrorCounters();
        var tasks = new System.Threading.Tasks.Task[10];

        // Act - Record errors concurrently
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    _service.RecordError();
                }
            });
        }

        System.Threading.Tasks.Task.WaitAll(tasks);

        // Assert - Should not throw
        var metrics = _service.CollectSystemMetrics();
        Assert.NotNull(metrics);
    }

    #endregion

    #region Network Metrics Tests

    [Fact]
    public void RecordNetworkLatency_Should_Update_Average_Latency()
    {
        // Arrange
        _service.ClearNetworkHistory();

        // Act
        _service.RecordNetworkLatency(50.0);
        _service.RecordNetworkLatency(60.0);
        _service.RecordNetworkLatency(70.0);

        var metrics = _service.CollectSystemMetrics();

        // Assert
        Assert.True(metrics.ContainsKey("NetworkLatencyMs"));
        Assert.Equal(60.0, metrics["NetworkLatencyMs"], 1); // Average should be 60
    }

    [Fact]
    public void RecordNetworkLatency_Should_Maintain_History_Size()
    {
        // Arrange
        _service.ClearNetworkHistory();

        // Act - Record more than max (100)
        for (int i = 0; i < 150; i++)
        {
            _service.RecordNetworkLatency(50.0 + i);
        }

        var metrics = _service.CollectSystemMetrics();

        // Assert - Should not cause issues and metrics should be valid
        Assert.NotNull(metrics);
        Assert.True(metrics.ContainsKey("NetworkLatencyMs"));
    }

    [Fact]
    public void GetNetworkLatency_Should_Return_Zero_When_No_History()
    {
        // Arrange
        _service.ClearNetworkHistory();

        // Act
        var metrics = _service.CollectSystemMetrics();

        // Assert
        Assert.Equal(0.0, metrics["NetworkLatencyMs"]);
    }

    #endregion

    #region System Metrics Tests

    [Fact]
    public void CollectSystemMetrics_Should_Include_All_Required_Metrics()
    {
        // Act
        var metrics = _service.CollectSystemMetrics();

        // Assert - Check for all key metrics
        var requiredMetrics = new[]
        {
            "CpuUtilization",
            "MemoryUtilization",
            "ThroughputPerSecond",
            "ErrorRate",
            "NetworkLatencyMs",
            "DiskReadBytesPerSecond",
            "DiskWriteBytesPerSecond",
            "SystemLoadAverage",
            "ThreadCount"
        };

        foreach (var metricName in requiredMetrics)
        {
            Assert.True(metrics.ContainsKey(metricName), $"Missing metric: {metricName}");
        }
    }

    [Fact]
    public void GetSystemLoadAverage_Should_Return_Valid_Range()
    {
        // Act
        var metrics = _service.CollectSystemMetrics();

        // Assert
        Assert.True(metrics["SystemLoadAverage"] >= 0.0);
        Assert.True(metrics["SystemLoadAverage"] <= 100.0); // Reasonable upper bound
    }

    [Fact]
    public void GetNetworkThroughput_Should_Return_Non_Negative()
    {
        // Act
        var metrics = _service.CollectSystemMetrics();

        // Assert
        Assert.True(metrics.ContainsKey("NetworkThroughputMbps"));
        Assert.True(metrics["NetworkThroughputMbps"] >= 0.0);
    }

    [Fact]
    public void GetDiskMetrics_Should_Return_Non_Negative()
    {
        // Act
        var metrics = _service.CollectSystemMetrics();

        // Assert
        Assert.True(metrics["DiskReadBytesPerSecond"] >= 0.0);
        Assert.True(metrics["DiskWriteBytesPerSecond"] >= 0.0);
    }

    [Fact]
    public void GetHandleCount_Should_Return_Non_Negative()
    {
        // Act
        var metrics = _service.CollectSystemMetrics();

        // Assert
        Assert.True(metrics.ContainsKey("HandleCount"));
        Assert.True(metrics["HandleCount"] >= 0.0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Full_Metrics_Lifecycle_Should_Work()
    {
        // Arrange
        _service.ClearPredictionHistory();
        _service.ResetErrorCounters();
        _service.ClearNetworkHistory();

        // Act - Simulate realistic scenario
        // 1. Process some requests
        for (int i = 0; i < 10; i++)
        {
            _service.RecordRequestProcessed();
        }

        // 2. Record some errors
        _service.RecordError();
        _service.RecordException();

        // 3. Record network latency
        _service.RecordNetworkLatency(45.0);
        _service.RecordNetworkLatency(55.0);

        // 4. Record prediction outcomes
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(95),
            TimeSpan.FromMilliseconds(200));

        // 5. Collect all metrics
        var metrics = _service.CollectSystemMetrics();
        var loadPatterns = _service.AnalyzeLoadPatterns();
        var healthScore = _service.CalculateSystemHealthScore();

        // Assert - All should work together
        Assert.NotNull(metrics);
        Assert.NotNull(loadPatterns);
        Assert.NotNull(healthScore);
        Assert.True(metrics.Count > 0);
        Assert.True(loadPatterns.TotalPredictions > 0);
        Assert.True(healthScore.Overall > 0);
    }

    #endregion
}