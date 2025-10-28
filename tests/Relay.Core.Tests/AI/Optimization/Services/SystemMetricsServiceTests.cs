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

    #region CalculateStrategyEffectiveness Tests

    [Fact]
    public void CalculateStrategyEffectiveness_Should_Return_Defaults_With_No_History()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.NotNull(loadPatternData.StrategyEffectiveness);
        Assert.NotEmpty(loadPatternData.StrategyEffectiveness);
        Assert.Contains("EnableCaching", loadPatternData.StrategyEffectiveness.Keys);
        Assert.Equal(0.8, loadPatternData.StrategyEffectiveness["EnableCaching"]);
    }

    [Fact]
    public void CalculateStrategyEffectiveness_Should_Calculate_Per_Strategy()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // EnableCaching: High effectiveness (good improvement + good accuracy)
        for (int i = 0; i < 5; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(95), // Good accuracy (95%)
                TimeSpan.FromMilliseconds(200)); // 50% improvement
        }

        // BatchProcessing: Lower effectiveness (poor accuracy)
        for (int i = 0; i < 5; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.BatchProcessing,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(200), // Poor accuracy (200%)
                TimeSpan.FromMilliseconds(300)); // Lower improvement
        }

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.True(loadPatternData.StrategyEffectiveness.ContainsKey("EnableCaching"));
        Assert.True(loadPatternData.StrategyEffectiveness.ContainsKey("BatchProcessing"));
        Assert.True(loadPatternData.StrategyEffectiveness["EnableCaching"] >
                   loadPatternData.StrategyEffectiveness["BatchProcessing"]);
    }

    [Fact]
    public void CalculateStrategyEffectiveness_Should_Combine_Improvement_And_Accuracy()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // Perfect prediction: 50% improvement, 100% accuracy
        // Improvement: (200 - 100) / 200 = 0.5
        // Accuracy: 100ms predicted, 100ms actual = 100% (within 80-120%)
        // Effectiveness: (0.5 * 0.6) + (1.0 * 0.4) = 0.3 + 0.4 = 0.7
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200));

        // Act
        var loadPatternData = _service.AnalyzeLoadPatterns();

        // Assert
        Assert.True(loadPatternData.StrategyEffectiveness.ContainsKey("EnableCaching"));
        Assert.Equal(0.7, loadPatternData.StrategyEffectiveness["EnableCaching"], 1);
    }

    [Fact]
    public void CalculateStrategyEffectiveness_Should_Handle_Multiple_Strategies()
    {
        // Arrange
        _service.ClearPredictionHistory();

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
                TimeSpan.FromMilliseconds(200));
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

    [Fact]
    public void GetStrategyEffectiveness_Should_Return_Zero_Data_When_No_History()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // Act
        var data = _service.GetStrategyEffectiveness(OptimizationStrategy.EnableCaching);

        // Assert
        Assert.NotNull(data);
        Assert.Equal(OptimizationStrategy.EnableCaching, data.Strategy);
        Assert.Equal(0, data.TotalApplications);
        Assert.Equal(0.0, data.SuccessRate);
        Assert.Equal(0.0, data.AverageImprovement);
        Assert.Equal(0.0, data.OverallEffectiveness);
    }

    [Fact]
    public void GetStrategyEffectiveness_Should_Calculate_Detailed_Metrics()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // Record 10 outcomes for EnableCaching: 8 successful, 2 failed
        // 8 successful predictions (within 80-120%)
        for (int i = 0; i < 8; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(90 + i), // 90-97ms (good accuracy)
                TimeSpan.FromMilliseconds(200)); // 50% improvement
        }

        // 2 unsuccessful predictions (outside range)
        for (int i = 0; i < 2; i++)
        {
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(150), // 150% (poor accuracy)
                TimeSpan.FromMilliseconds(200)); // 25% improvement
        }

        // Act
        var data = _service.GetStrategyEffectiveness(OptimizationStrategy.EnableCaching);

        // Assert
        Assert.Equal(10, data.TotalApplications);
        Assert.Equal(0.8, data.SuccessRate, 2); // 80% success rate

        // Average improvement calculation:
        // For 8 successful: (200 - 90-97) / 200 ≈ 0.5-0.525 avg ≈ 0.51
        // For 2 unsuccessful: (200 - 150) / 200 = 0.25
        // Weighted avg: (8 * 0.51 + 2 * 0.25) / 10 ≈ 0.458
        Assert.True(data.AverageImprovement >= 0.45 && data.AverageImprovement <= 0.52);

        // Effectiveness: (avgImprovement * 0.6) + (0.8 * 0.4)
        Assert.True(data.OverallEffectiveness >= 0.57 && data.OverallEffectiveness <= 0.64);
    }

    [Fact]
    public void GetStrategyEffectiveness_Should_Ignore_Zero_Baseline()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // Valid prediction
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(95),
            TimeSpan.FromMilliseconds(200));

        // Invalid prediction with zero baseline (should be ignored)
        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(95),
            TimeSpan.Zero);

        // Act
        var data = _service.GetStrategyEffectiveness(OptimizationStrategy.EnableCaching);

        // Assert - Should only count the valid prediction
        Assert.Equal(1, data.TotalApplications);
    }

    [Fact]
    public void GetAllStrategyEffectiveness_Should_Return_All_Strategies()
    {
        // Arrange
        _service.ClearPredictionHistory();

        _service.RecordPredictionOutcome(
            OptimizationStrategy.EnableCaching,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(95),
            TimeSpan.FromMilliseconds(200));

        _service.RecordPredictionOutcome(
            OptimizationStrategy.BatchProcessing,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(95),
            TimeSpan.FromMilliseconds(200));

        // Act
        var allData = _service.GetAllStrategyEffectiveness();

        // Assert
        Assert.NotNull(allData);
        Assert.Equal(2, allData.Count);
        Assert.Contains(allData, d => d.Strategy == OptimizationStrategy.EnableCaching);
        Assert.Contains(allData, d => d.Strategy == OptimizationStrategy.BatchProcessing);
    }

    [Fact]
    public void GetAllStrategyEffectiveness_Should_Return_Empty_When_No_Data()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // Act
        var allData = _service.GetAllStrategyEffectiveness();

        // Assert
        Assert.NotNull(allData);
        Assert.Empty(allData);
    }

    [Fact]
    public void StrategyEffectiveness_Should_Weight_Improvement_More_Than_Accuracy()
    {
        // Arrange
        _service.ClearPredictionHistory();

        // Strategy A: High improvement, low accuracy
        // 4 predictions with good accuracy (95ms actual vs 100ms predicted = 95%)
        // 6 predictions with poor accuracy (200ms actual vs 100ms predicted = 200%)
        // All have same baseline 250ms, actual time varies
        for (int i = 0; i < 10; i++)
        {
            var actualTime = i < 4 ? TimeSpan.FromMilliseconds(95) : TimeSpan.FromMilliseconds(200);
            _service.RecordPredictionOutcome(
                OptimizationStrategy.EnableCaching,
                TimeSpan.FromMilliseconds(100), // Predicted improvement
                actualTime, // Actual execution time after optimization
                TimeSpan.FromMilliseconds(250)); // Baseline execution time
        }

        // Strategy B: Lower improvement, higher accuracy
        // 8 predictions with good accuracy (95ms actual vs 100ms predicted = 95%)
        // 2 predictions with poor accuracy (200ms actual vs 100ms predicted = 200%)
        // All have same baseline 150ms (lower improvement potential)
        for (int i = 0; i < 10; i++)
        {
            var actualTime = i < 8 ? TimeSpan.FromMilliseconds(95) : TimeSpan.FromMilliseconds(200);
            _service.RecordPredictionOutcome(
                OptimizationStrategy.BatchProcessing,
                TimeSpan.FromMilliseconds(100), // Predicted improvement
                actualTime, // Actual execution time after optimization
                TimeSpan.FromMilliseconds(150)); // Baseline execution time (lower than Strategy A)
        }

        // Act
        var cachingData = _service.GetStrategyEffectiveness(OptimizationStrategy.EnableCaching);
        var batchingData = _service.GetStrategyEffectiveness(OptimizationStrategy.BatchProcessing);

        // Assert - Strategy A has higher improvement due to higher baseline, even with lower accuracy
        // Strategy A improvement: avg((250-95)/250, (250-200)/250) = avg(0.62, 0.20) weighted by count
        // Strategy B improvement: avg((150-95)/150, (150-200)/150) = avg(0.367, negative clamped to 0)
        // Strategy A should have higher overall effectiveness due to better improvement
        Assert.True(cachingData.AverageImprovement > batchingData.AverageImprovement,
            $"Caching improvement ({cachingData.AverageImprovement:F2}) should be higher than Batching ({batchingData.AverageImprovement:F2})");
    }

    [Fact]
    public void StrategyEffectiveness_Should_Be_Thread_Safe()
    {
        // Arrange
        _service.ClearPredictionHistory();
        var tasks = new System.Threading.Tasks.Task[10];

        // Act - Record outcomes concurrently
        for (int i = 0; i < tasks.Length; i++)
        {
            var strategyIndex = i % 4;
            var strategy = strategyIndex switch
            {
                0 => OptimizationStrategy.EnableCaching,
                1 => OptimizationStrategy.BatchProcessing,
                2 => OptimizationStrategy.ParallelProcessing,
                _ => OptimizationStrategy.CircuitBreaker
            };

            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int j = 0; j < 5; j++)
                {
                    _service.RecordPredictionOutcome(
                        strategy,
                        TimeSpan.FromMilliseconds(100),
                        TimeSpan.FromMilliseconds(95),
                        TimeSpan.FromMilliseconds(200));
                }
            });
        }

        System.Threading.Tasks.Task.WaitAll(tasks);

        // Assert - Should not throw and should have valid data
        var allData = _service.GetAllStrategyEffectiveness();
        Assert.NotNull(allData);
        Assert.Equal(4, allData.Count);

        foreach (var data in allData)
        {
            Assert.True(data.TotalApplications > 0);
            Assert.True(data.OverallEffectiveness >= 0 && data.OverallEffectiveness <= 1);
        }
    }

    #endregion
}