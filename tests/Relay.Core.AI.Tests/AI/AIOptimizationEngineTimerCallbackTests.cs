using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AIOptimizationEngineTimerCallbackTests : IDisposable
{
    private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
    private readonly AIOptimizationOptions _options;
    private AIOptimizationEngine _engine;

    public AIOptimizationEngineTimerCallbackTests()
    {
        _loggerMock = new Mock<ILogger<AIOptimizationEngine>>();
        _options = new AIOptimizationOptions
        {
            DefaultBatchSize = 10,
            MaxBatchSize = 100,
            ModelUpdateInterval = TimeSpan.FromMinutes(5),
            ModelTrainingDate = DateTime.UtcNow,
            ModelVersion = "1.0.0",
            LastRetrainingDate = DateTime.UtcNow.AddDays(-1)
        };

        var optionsMock = new Mock<IOptions<AIOptimizationOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        // Create mock dependencies
        var metricsAggregatorMock = new Mock<IMetricsAggregator>();
        var healthScorerMock = new Mock<IHealthScorer>();
        var systemAnalyzerMock = new Mock<ISystemAnalyzer>();
        var metricsPublisherMock = new Mock<IMetricsPublisher>();
        var metricsOptions = new MetricsCollectionOptions();
        var healthOptions = new HealthScoringOptions();

        // Setup default mock behaviors
        metricsAggregatorMock.Setup(x => x.CollectAllMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, IEnumerable<MetricValue>>());
        metricsAggregatorMock.Setup(x => x.GetLatestMetrics())
            .Returns(new Dictionary<string, IEnumerable<MetricValue>>
            {
                ["ThroughputPerSecond"] = new[] { new MetricValue { Name = "ThroughputPerSecond", Value = 100.0, Timestamp = DateTime.UtcNow } },
                ["MemoryUtilization"] = new[] { new MetricValue { Name = "MemoryUtilization", Value = 0.5, Timestamp = DateTime.UtcNow } },
                ["ErrorRate"] = new[] { new MetricValue { Name = "ErrorRate", Value = 0.01, Timestamp = DateTime.UtcNow } }
            });
        healthScorerMock.Setup(x => x.CalculateScoreAsync(It.IsAny<Dictionary<string, double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.8);
        systemAnalyzerMock.Setup(x => x.AnalyzeLoadPatternsAsync(It.IsAny<Dictionary<string, double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadPatternData { Level = LoadLevel.Medium });

        _engine = new AIOptimizationEngine(
            _loggerMock.Object,
            optionsMock.Object,
            metricsAggregatorMock.Object,
            healthScorerMock.Object,
            systemAnalyzerMock.Object,
            metricsPublisherMock.Object,
            metricsOptions,
            healthOptions);
    }

    public void Dispose()
    {
        _engine?.Dispose();
    }

    [Fact]
    public void UpdateModelCallback_Should_Return_Early_When_Disposed()
    {
        // Arrange
        _engine.Dispose();

        // Get the private method using reflection
        var updateMethod = typeof(AIOptimizationEngine).GetMethod("UpdateModelCallback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(updateMethod);

        // Act
        updateMethod!.Invoke(_engine, new object[] { null });

        // Assert - Should not throw and should return early (no logging should occur for the main logic)
        // The disposed check happens before any significant work
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Updating AI model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void UpdateModelCallback_Should_Return_Early_When_Learning_Disabled()
    {
        // Arrange
        _engine.SetLearningMode(false);

        // Get the private method using reflection
        var updateMethod = typeof(AIOptimizationEngine).GetMethod("UpdateModelCallback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(updateMethod);

        // Act
        updateMethod!.Invoke(_engine, new object[] { null });

        // Assert - Should not throw and should return early
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Updating AI model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void UpdateModelCallback_Should_Log_Error_When_Exception_Occurs()
    {
        // Arrange - Set up metrics aggregator to throw exception
        var metricsAggregatorMock = new Mock<IMetricsAggregator>();
        metricsAggregatorMock.Setup(x => x.GetLatestMetrics())
            .Throws(new InvalidOperationException("Test exception"));

        // Get the private field and set it (this is a bit hacky but necessary for testing)
        var aggregatorField = typeof(AIOptimizationEngine).GetField("_metricsAggregator",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        aggregatorField!.SetValue(_engine, metricsAggregatorMock.Object);

        // Get the private method using reflection
        var updateMethod = typeof(AIOptimizationEngine).GetMethod("UpdateModelCallback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(updateMethod);

        // Act
        updateMethod!.Invoke(_engine, new object[] { null });

        // Assert - Should log the error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error during AI model update")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CollectMetricsCallback_Should_Return_Early_When_Disposed()
    {
        // Arrange
        _engine.Dispose();

        // Get the private method using reflection
        var collectMethod = typeof(AIOptimizationEngine).GetMethod("CollectMetricsCallback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(collectMethod);

        // Act
        collectMethod!.Invoke(_engine, new object[] { null });

        // Assert - Should not throw and should return early
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Collected and analyzed AI metrics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void CollectMetricsCallback_Should_Log_Warning_When_Exception_Occurs()
    {
        // Arrange - Set up metrics aggregator to throw exception
        var metricsAggregatorMock = new Mock<IMetricsAggregator>();
        metricsAggregatorMock.Setup(x => x.GetLatestMetrics())
            .Throws(new InvalidOperationException("Test exception"));

        // Get the private field and set it
        var aggregatorField = typeof(AIOptimizationEngine).GetField("_metricsAggregator",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        aggregatorField!.SetValue(_engine, metricsAggregatorMock.Object);

        // Get the private method using reflection
        var collectMethod = typeof(AIOptimizationEngine).GetMethod("CollectMetricsCallback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(collectMethod);

        // Act
        collectMethod!.Invoke(_engine, new object[] { null });

        // Assert - Should log the warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error collecting AI metrics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdateModelCallback_Should_Execute_Successfully_With_Valid_Data()
    {
        // Arrange - Ensure we have some time series data
        // Get the time series database and add some data
        var tsDbField = typeof(AIOptimizationEngine).GetField("_timeSeriesDb",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tsDb = tsDbField!.GetValue(_engine) as Relay.Core.AI.Analysis.TimeSeries.TimeSeriesDatabase;

        // Add 25 hours of data (more than 24 needed for the check)
        for (int i = 0; i < 25; i++)
        {
            tsDb!.StoreMetric("ThroughputPerSecond", 100.0 + i, DateTime.UtcNow.AddHours(-25 + i));
        }

        // Get the private method using reflection
        var updateMethod = typeof(AIOptimizationEngine).GetMethod("UpdateModelCallback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(updateMethod);

        // Act
        updateMethod!.Invoke(_engine, new object[] { null });

        // Assert - Should log successful completion
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI model update completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CollectMetricsCallback_Should_Execute_Successfully_With_Valid_Data()
    {
        // Get the private method using reflection
        var collectMethod = typeof(AIOptimizationEngine).GetMethod("CollectMetricsCallback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(collectMethod);

        // Act
        collectMethod!.Invoke(_engine, new object[] { null });

        // Assert - Should log successful completion
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Collected and analyzed AI metrics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}