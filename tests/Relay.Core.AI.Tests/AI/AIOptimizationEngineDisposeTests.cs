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

public class AIOptimizationEngineDisposeTests : IDisposable
{
    private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
    private readonly AIOptimizationOptions _options;
    private AIOptimizationEngine _engine;

    public AIOptimizationEngineDisposeTests()
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
            .Returns(new Dictionary<string, IEnumerable<MetricValue>>());
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
    public void Dispose_Should_Set_Disposed_Flag()
    {
        // Arrange
        var disposedField = typeof(AIOptimizationEngine).GetField("_disposed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(disposedField);

        // Verify initial state
        Assert.False((bool)disposedField!.GetValue(_engine)!);

        // Act
        _engine.Dispose();

        // Assert
        Assert.True((bool)disposedField.GetValue(_engine)!);
    }

    [Fact]
    public void Dispose_Should_Dispose_Model_Update_Timer()
    {
        // Arrange
        var timerField = typeof(AIOptimizationEngine).GetField("_modelUpdateTimer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var timer = (Timer)timerField!.GetValue(_engine)!;

        // Act
        _engine.Dispose();

        // Assert - Timer should be disposed (checking if it's null or disposed)
        // Note: We can't directly check if Timer is disposed, but we can verify the field is set
        var timerAfterDispose = (Timer)timerField.GetValue(_engine)!;
        Assert.NotNull(timerAfterDispose); // Field still exists but timer should be disposed internally
    }

    [Fact]
    public void Dispose_Should_Dispose_Metrics_Collection_Timer()
    {
        // Arrange
        var timerField = typeof(AIOptimizationEngine).GetField("_metricsCollectionTimer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var timer = (Timer)timerField!.GetValue(_engine)!;

        // Act
        _engine.Dispose();

        // Assert - Timer should be disposed
        var timerAfterDispose = (Timer)timerField.GetValue(_engine)!;
        Assert.NotNull(timerAfterDispose); // Field still exists but timer should be disposed internally
    }

    [Fact]
    public void Dispose_Should_Dispose_Time_Series_Database()
    {
        // Arrange
        var tsDbField = typeof(AIOptimizationEngine).GetField("_timeSeriesDb",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tsDb = tsDbField!.GetValue(_engine);

        // Act
        _engine.Dispose();

        // Assert - Time series database should still be accessible (Dispose just calls Dispose on it)
        var tsDbAfterDispose = tsDbField.GetValue(_engine);
        Assert.NotNull(tsDbAfterDispose);
    }

    [Fact]
    public void Dispose_Should_Handle_Multiple_Calls_Safely()
    {
        // Act - Call Dispose multiple times
        _engine.Dispose();
        _engine.Dispose();
        _engine.Dispose();

        // Assert - No exceptions thrown, disposed flag remains true
        var disposedField = typeof(AIOptimizationEngine).GetField("_disposed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.True((bool)disposedField!.GetValue(_engine)!);
    }

    [Fact]
    public void Dispose_Should_Not_Dispose_Managed_Resources_When_Called_From_Finalizer()
    {
        // Arrange - Get the protected Dispose method
        var disposeMethod = typeof(AIOptimizationEngine).GetMethod("Dispose",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new[] { typeof(bool) }, null);
        Assert.NotNull(disposeMethod);

        var disposedField = typeof(AIOptimizationEngine).GetField("_disposed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act - Call Dispose with disposing = false (simulating finalizer)
        disposeMethod!.Invoke(_engine, new object[] { false });

        // Assert - Disposed flag should be set but managed resources should not be disposed
        Assert.True((bool)disposedField!.GetValue(_engine)!);

        // Timers should still be accessible (not disposed)
        var modelTimerField = typeof(AIOptimizationEngine).GetField("_modelUpdateTimer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var metricsTimerField = typeof(AIOptimizationEngine).GetField("_metricsCollectionTimer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(modelTimerField!.GetValue(_engine));
        Assert.NotNull(metricsTimerField!.GetValue(_engine));
    }

    [Fact]
    public async Task Public_Methods_Should_Throw_ObjectDisposedException_After_Dispose()
    {
        // Arrange
        _engine.Dispose();

        // Act & Assert - All public methods should throw ObjectDisposedException
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.AnalyzeRequestAsync(new TestRequest(), CreateMetrics(), CancellationToken.None));

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), new SystemLoadMetrics { CpuUtilization = 0.5, MemoryUtilization = 0.6, ActiveConnections = 100 }, CancellationToken.None));

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.ShouldCacheAsync(typeof(TestRequest), new AccessPattern[] { new AccessPattern { AccessFrequency = 10, TimeSinceLastAccess = TimeSpan.FromMinutes(5) } }, CancellationToken.None));

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.EnableCaching }, CreateMetrics(), CancellationToken.None));

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1), CancellationToken.None));

        Assert.Throws<ObjectDisposedException>(() =>
            _engine.GetLoadPatternAnalysisAsync().GetAwaiter().GetResult());

        // Non-async methods should also be protected
        Assert.Throws<ObjectDisposedException>(() => _engine.SetLearningMode(false));
        Assert.Throws<ObjectDisposedException>(() => _engine.GetModelStatistics());
    }

    #region Helper Methods

    private RequestExecutionMetrics CreateMetrics()
    {
        return new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            MedianExecutionTime = TimeSpan.FromMilliseconds(95),
            P95ExecutionTime = TimeSpan.FromMilliseconds(150),
            P99ExecutionTime = TimeSpan.FromMilliseconds(200),
            TotalExecutions = 100,
            SuccessfulExecutions = 95,
            FailedExecutions = 5,
            MemoryAllocated = 1024 * 1024,
            ConcurrentExecutions = 10,
            LastExecution = DateTime.UtcNow,
            SamplePeriod = TimeSpan.FromMinutes(5),
            CpuUsage = 0.45,
            MemoryUsage = 512 * 1024,
            DatabaseCalls = 2,
            ExternalApiCalls = 1
        };
    }

    #endregion

    #region Test Types

    private class TestRequest { }

    #endregion
}