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

public class AIOptimizationEngineDisposedTests : IDisposable
{
    private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
    private readonly AIOptimizationOptions _options;
    private AIOptimizationEngine _engine;

    public AIOptimizationEngineDisposedTests()
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
    public async Task AnalyzeRequestAsync_Should_Throw_ObjectDisposedException_When_Disposed()
    {
        // Arrange
        _engine.Dispose();
        var request = new TestRequest();
        var metrics = CreateMetrics();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.AnalyzeRequestAsync(request, metrics, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Throw_ObjectDisposedException_When_Disposed()
    {
        // Arrange
        _engine.Dispose();
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.6,
            ActiveConnections = 100
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), currentLoad, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Throw_ObjectDisposedException_When_Disposed()
    {
        // Arrange
        _engine.Dispose();
        var accessPatterns = new AccessPattern[]
        {
            new AccessPattern { AccessFrequency = 10, TimeSinceLastAccess = TimeSpan.FromMinutes(5) }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task LearnFromExecutionAsync_Should_Throw_ObjectDisposedException_When_Disposed()
    {
        // Arrange
        _engine.Dispose();
        var appliedOptimizations = new OptimizationStrategy[] { OptimizationStrategy.EnableCaching };
        var actualMetrics = CreateMetrics();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), appliedOptimizations, actualMetrics, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Throw_ObjectDisposedException_When_Disposed()
    {
        // Arrange
        _engine.Dispose();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1), CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GetLoadPatternAnalysisAsync_Should_Throw_ObjectDisposedException_When_Disposed()
    {
        // Arrange
        _engine.Dispose();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.GetLoadPatternAnalysisAsync());
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Throw_ArgumentNullException_For_Null_Request()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.AnalyzeRequestAsync<TestRequest>(null!, CreateMetrics(), CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Throw_ArgumentNullException_For_Null_Metrics()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.AnalyzeRequestAsync(request, null!, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Throw_ArgumentNullException_For_Null_RequestType()
    {
        // Arrange
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.6,
            ActiveConnections = 100
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.PredictOptimalBatchSizeAsync(null!, currentLoad, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Throw_ArgumentNullException_For_Null_CurrentLoad()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), null!, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Throw_ArgumentNullException_For_Null_RequestType()
    {
        // Arrange
        var accessPatterns = new AccessPattern[]
        {
            new AccessPattern { AccessFrequency = 10, TimeSinceLastAccess = TimeSpan.FromMinutes(5) }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.ShouldCacheAsync(null!, accessPatterns, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ShouldCacheAsync_Should_Throw_ArgumentNullException_For_Null_AccessPatterns()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.ShouldCacheAsync(typeof(TestRequest), null!, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task LearnFromExecutionAsync_Should_Throw_ArgumentNullException_For_Null_RequestType()
    {
        // Arrange
        var appliedOptimizations = new OptimizationStrategy[] { OptimizationStrategy.EnableCaching };
        var actualMetrics = CreateMetrics();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.LearnFromExecutionAsync(null!, appliedOptimizations, actualMetrics, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task LearnFromExecutionAsync_Should_Throw_ArgumentNullException_For_Null_AppliedOptimizations()
    {
        // Arrange
        var actualMetrics = CreateMetrics();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), null!, actualMetrics, CancellationToken.None));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task LearnFromExecutionAsync_Should_Throw_ArgumentNullException_For_Null_ActualMetrics()
    {
        // Arrange
        var appliedOptimizations = new OptimizationStrategy[] { OptimizationStrategy.EnableCaching };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), appliedOptimizations, null!, CancellationToken.None));
        Assert.NotNull(exception);
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