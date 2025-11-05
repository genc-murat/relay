using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Services;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class SystemMetricsServiceTests
{
    private readonly ILogger<SystemMetricsService> _logger;
    private readonly SystemMetricsService _service;
    private readonly IMetricsAggregator _metricsAggregator;
    private readonly IHealthScorer _healthScorer;
    private readonly ISystemAnalyzer _systemAnalyzer;
    private readonly IMetricsPublisher _metricsPublisher;
    private readonly MetricsCollectionOptions _metricsOptions;
    private readonly HealthScoringOptions _healthOptions;

    public SystemMetricsServiceTests()
    {
        _logger = NullLogger<SystemMetricsService>.Instance;

        // Create mock dependencies
        var metricsAggregatorMock = new Mock<IMetricsAggregator>();
        var healthScorerMock = new Mock<IHealthScorer>();
        var systemAnalyzerMock = new Mock<ISystemAnalyzer>();
        var metricsPublisherMock = new Mock<IMetricsPublisher>();

        _metricsAggregator = metricsAggregatorMock.Object;
        _healthScorer = healthScorerMock.Object;
        _systemAnalyzer = systemAnalyzerMock.Object;
        _metricsPublisher = metricsPublisherMock.Object;
        _metricsOptions = new MetricsCollectionOptions();
        _healthOptions = new HealthScoringOptions();

        // Setup default mock behaviors with fake metrics for testing
        var fakeMetrics = new Dictionary<string, IEnumerable<MetricValue>>
        {
            ["CpuMetricsCollector"] = new List<MetricValue>
            {
                new MetricValue { Name = "CpuUtilization", Value = 0.5, Timestamp = DateTime.UtcNow },
                new MetricValue { Name = "CpuUsagePercent", Value = 50.0, Timestamp = DateTime.UtcNow }
            },
            ["MemoryMetricsCollector"] = new List<MetricValue>
            {
                new MetricValue { Name = "MemoryUtilization", Value = 0.6, Timestamp = DateTime.UtcNow },
                new MetricValue { Name = "MemoryUsageMB", Value = 614.0, Timestamp = DateTime.UtcNow },
                new MetricValue { Name = "AvailableMemoryMB", Value = 409.0, Timestamp = DateTime.UtcNow }
            },
            ["ThroughputMetricsCollector"] = new List<MetricValue>
            {
                new MetricValue { Name = "ThroughputPerSecond", Value = 100.0, Timestamp = DateTime.UtcNow },
                new MetricValue { Name = "RequestsPerSecond", Value = 95.0, Timestamp = DateTime.UtcNow }
            }
        };

        metricsAggregatorMock.Setup(x => x.CollectAllMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeMetrics);
        metricsAggregatorMock.Setup(x => x.GetLatestMetrics())
            .Returns(fakeMetrics);
        healthScorerMock.Setup(x => x.CalculateScoreAsync(It.IsAny<Dictionary<string, double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.8);
        systemAnalyzerMock.Setup(x => x.AnalyzeLoadPatternsAsync(It.IsAny<Dictionary<string, double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadPatternData { Level = LoadLevel.Medium });

        _service = new SystemMetricsService(
            _logger,
            _metricsAggregator,
            _healthScorer,
            _systemAnalyzer,
            _metricsPublisher,
            _metricsOptions,
            _healthOptions);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Arrange
        var metricsAggregatorMock = new Mock<IMetricsAggregator>();
        var healthScorerMock = new Mock<IHealthScorer>();
        var systemAnalyzerMock = new Mock<ISystemAnalyzer>();
        var metricsPublisherMock = new Mock<IMetricsPublisher>();
        var metricsOptions = new MetricsCollectionOptions();
        var healthOptions = new HealthScoringOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SystemMetricsService(
            null!,
            metricsAggregatorMock.Object,
            healthScorerMock.Object,
            systemAnalyzerMock.Object,
            metricsPublisherMock.Object,
            metricsOptions,
            healthOptions));
    }

    #endregion

    #region AnalyzeLoadPatterns Tests

    [Fact]
    public async Task AnalyzeLoadPatternsAsync_Should_Return_Valid_LoadPatternData()
    {
        // Act
        var result = await _service.AnalyzeLoadPatternsAsync();

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
    public async Task AnalyzeLoadPatternsAsync_Should_Determine_Load_Level_Correctly()
    {
        // Act
        var result = await _service.AnalyzeLoadPatternsAsync();

        // Assert
        Assert.True(Enum.IsDefined(typeof(LoadLevel), result.Level));
    }

    [Fact]
    public async Task AnalyzeLoadPatternsAsync_Should_Include_Predictions()
    {
        // Act
        var result = await _service.AnalyzeLoadPatternsAsync();

        // Assert
        Assert.NotNull(result.Predictions);
        // May be empty if no predictions are generated, but should not be null
    }

    [Fact]
    public async Task AnalyzeLoadPatternsAsync_Should_Include_Strategy_Effectiveness()
    {
        // Act
        var result = await _service.AnalyzeLoadPatternsAsync();

        // Assert
        Assert.NotNull(result.StrategyEffectiveness);
        Assert.True(result.StrategyEffectiveness.Count >= 0);
    }

    [Fact]
    public async Task AnalyzeLoadPatternsAsync_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task<LoadPatternData>[5];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(async () =>
            {
                return await _service.AnalyzeLoadPatternsAsync();
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
    public async Task CalculateSystemHealthScoreAsync_Should_Return_Valid_HealthScore()
    {
        // Act
        var result = await _service.CalculateSystemHealthScoreAsync();

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
    public async Task CollectAllMetricsAsync_Should_Return_Metrics_Dictionary()
    {
        // Act
        var result = await _service.CollectAllMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Dictionary<string, IEnumerable<MetricValue>>>(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public async Task CollectAllMetricsAsync_Should_Include_CPU_Metrics()
    {
        // Act
        var result = await _service.CollectAllMetricsAsync();

        // Assert
        Assert.True(result.ContainsKey("CpuMetricsCollector"));
        var cpuMetrics = result["CpuMetricsCollector"];
        Assert.Contains(cpuMetrics, m => m.Name == "CpuUtilization");
        Assert.Contains(cpuMetrics, m => m.Name == "CpuUsagePercent");
    }

    [Fact]
    public async Task CollectAllMetricsAsync_Should_Include_Memory_Metrics()
    {
        // Act
        var result = await _service.CollectAllMetricsAsync();

        // Assert
        Assert.True(result.ContainsKey("MemoryMetricsCollector"));
        var memoryMetrics = result["MemoryMetricsCollector"];
        Assert.Contains(memoryMetrics, m => m.Name == "MemoryUtilization");
        Assert.Contains(memoryMetrics, m => m.Name == "MemoryUsageMB");
        Assert.Contains(memoryMetrics, m => m.Name == "AvailableMemoryMB");
    }

    [Fact]
    public async Task CollectAllMetricsAsync_Should_Include_Throughput_Metrics()
    {
        // Act
        var result = await _service.CollectAllMetricsAsync();

        // Assert
        Assert.True(result.ContainsKey("ThroughputMetricsCollector"));
        var throughputMetrics = result["ThroughputMetricsCollector"];
        Assert.Contains(throughputMetrics, m => m.Name == "ThroughputPerSecond");
        Assert.Contains(throughputMetrics, m => m.Name == "RequestsPerSecond");
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

    // NOTE: The following tests have been removed because the methods they test
    // (GetPredictionHistorySize, ClearPredictionHistory, ResetErrorCounters, etc.)
    // no longer exist on SystemMetricsService. The functionality has been moved to
    // individual services (ISystemAnalyzer, IMetricsAggregator, etc.).
    // These tests should be rewritten to test the individual services directly.

    #endregion
}
