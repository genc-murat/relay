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

public class AIOptimizationEngineLoadPatternTests : IDisposable
{
    private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
    private readonly AIOptimizationOptions _options;
    private AIOptimizationEngine _engine;

    public AIOptimizationEngineLoadPatternTests()
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
    public async Task GetLoadPatternAnalysisAsync_Should_Return_LoadPatternData()
    {
        // Act
        var result = await _engine.GetLoadPatternAnalysisAsync();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<LoadPatternData>(result);
        Assert.True(result.TotalPredictions >= 0);
        Assert.True(result.SuccessRate >= 0 && result.SuccessRate <= 1.0);
        Assert.True(result.AverageImprovement >= 0);
    }

    [Fact]
    public async Task GetLoadPatternAnalysisAsync_Should_Combine_System_And_Predictive_Load_Data()
    {
        // Arrange - Set up different load levels for system and predictive analysis
        var systemAnalyzerMock = new Mock<ISystemAnalyzer>();
        systemAnalyzerMock.Setup(x => x.AnalyzeLoadPatternsAsync(It.IsAny<Dictionary<string, double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadPatternData
            {
                Level = LoadLevel.High,
                Predictions = new List<PredictionResult> { new PredictionResult { RequestType = typeof(TestRequest), PredictedStrategies = new[] { OptimizationStrategy.EnableCaching } } },
                SuccessRate = 0.75,
                AverageImprovement = 0.2,
                TotalPredictions = 10
            });

        // Replace the system analyzer in the SystemMetricsService
        var systemMetricsServiceField = typeof(AIOptimizationEngine).GetField("_systemMetricsService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var systemMetricsService = systemMetricsServiceField!.GetValue(_engine) as SystemMetricsService;
        var systemAnalyzerField = typeof(SystemMetricsService).GetField("_systemAnalyzer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        systemAnalyzerField!.SetValue(systemMetricsService, systemAnalyzerMock.Object);

        // Mock the predictive analysis service to return different data
        var predictiveAnalysisServiceField = typeof(AIOptimizationEngine).GetField("_predictiveAnalysisService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var predictiveAnalysisServiceMock = new Mock<PredictiveAnalysisService>(Mock.Of<ILogger<PredictiveAnalysisService>>());
        predictiveAnalysisServiceMock.Setup(x => x.AnalyzeLoadPatterns())
            .Returns(new LoadPatternData
            {
                Level = LoadLevel.Low,
                Predictions = new List<PredictionResult> { new PredictionResult { RequestType = typeof(TestRequest), PredictedStrategies = new[] { OptimizationStrategy.BatchProcessing } } },
                SuccessRate = 0.85,
                AverageImprovement = 0.15,
                TotalPredictions = 5
            });
        predictiveAnalysisServiceField!.SetValue(_engine, predictiveAnalysisServiceMock.Object);

        // Act
        var result = await _engine.GetLoadPatternAnalysisAsync();

        // Assert - Should combine the data (higher level wins, predictions merged, weighted averages)
        Assert.Equal(LoadLevel.High, result.Level); // Higher level wins
        Assert.Equal(2, result.Predictions.Count); // Predictions merged
        Assert.Equal(15, result.TotalPredictions); // Total predictions summed
        Assert.True(result.SuccessRate > 0.75 && result.SuccessRate < 0.85); // Weighted average
    }

    [Fact]
    public async Task GetLoadPatternAnalysisAsync_Should_Handle_CancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        var result = await _engine.GetLoadPatternAnalysisAsync(cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetLoadPatternAnalysisAsync_Should_Cancel_When_Token_Is_Cancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _engine.GetLoadPatternAnalysisAsync(cts.Token));
    }

    [Fact]
    public async Task GetLoadPatternAnalysisAsync_Should_Return_Valid_Strategy_Effectiveness()
    {
        // Act
        var result = await _engine.GetLoadPatternAnalysisAsync();

        // Assert
        Assert.NotNull(result.StrategyEffectiveness);
        // Strategy effectiveness should be a dictionary, even if empty
        Assert.IsType<Dictionary<string, double>>(result.StrategyEffectiveness);
    }

    [Fact]
    public async Task GetLoadPatternAnalysisAsync_Should_Merge_Strategy_Effectiveness_Data()
    {
        // Arrange - Set up system analyzer with strategy effectiveness
        var systemAnalyzerMock = new Mock<ISystemAnalyzer>();
        var systemStrategyEffectiveness = new Dictionary<string, double>
        {
            ["Caching"] = 0.8,
            ["Batching"] = 0.6
        };

        systemAnalyzerMock.Setup(x => x.AnalyzeLoadPatternsAsync(It.IsAny<Dictionary<string, double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadPatternData
            {
                Level = LoadLevel.Medium,
                StrategyEffectiveness = systemStrategyEffectiveness
            });

        // Replace the system analyzer in the SystemMetricsService
        var systemMetricsServiceField = typeof(AIOptimizationEngine).GetField("_systemMetricsService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var systemMetricsService = systemMetricsServiceField!.GetValue(_engine) as SystemMetricsService;
        var systemAnalyzerField = typeof(SystemMetricsService).GetField("_systemAnalyzer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        systemAnalyzerField!.SetValue(systemMetricsService, systemAnalyzerMock.Object);

        // Mock predictive analysis with overlapping strategy
        var predictiveAnalysisServiceField = typeof(AIOptimizationEngine).GetField("_predictiveAnalysisService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var predictiveAnalysisServiceMock = new Mock<PredictiveAnalysisService>(Mock.Of<ILogger<PredictiveAnalysisService>>());
        var predictiveStrategyEffectiveness = new Dictionary<string, double>
        {
            ["Caching"] = 0.9, // Overlapping strategy
            ["Pooling"] = 0.7  // New strategy
        };

        predictiveAnalysisServiceMock.Setup(x => x.AnalyzeLoadPatterns())
            .Returns(new LoadPatternData
            {
                Level = LoadLevel.Medium,
                StrategyEffectiveness = predictiveStrategyEffectiveness
            });
        predictiveAnalysisServiceField!.SetValue(_engine, predictiveAnalysisServiceMock.Object);

        // Act
        var result = await _engine.GetLoadPatternAnalysisAsync();

        // Assert - Strategy effectiveness should be merged with averages for overlapping keys
        Assert.Contains("Caching", result.StrategyEffectiveness.Keys);
        Assert.Contains("Batching", result.StrategyEffectiveness.Keys);
        Assert.Contains("Pooling", result.StrategyEffectiveness.Keys);

        // Caching should be averaged: (0.8 + 0.9) / 2 = 0.85
        Assert.Equal(0.85, result.StrategyEffectiveness["Caching"], 0.001);
    }

    [Fact]
    public async Task GetLoadPatternAnalysisAsync_Should_Handle_Empty_Predictions()
    {
        // Arrange - Set up mocks that return empty predictions
        var systemAnalyzerMock = new Mock<ISystemAnalyzer>();
        systemAnalyzerMock.Setup(x => x.AnalyzeLoadPatternsAsync(It.IsAny<Dictionary<string, double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadPatternData
            {
                Level = LoadLevel.Low,
                Predictions = new List<PredictionResult>(),
                SuccessRate = 0.0,
                AverageImprovement = 0.0,
                TotalPredictions = 0
            });

        // Replace the system analyzer in the SystemMetricsService
        var systemMetricsServiceField = typeof(AIOptimizationEngine).GetField("_systemMetricsService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var systemMetricsService = systemMetricsServiceField!.GetValue(_engine) as SystemMetricsService;
        var systemAnalyzerField = typeof(SystemMetricsService).GetField("_systemAnalyzer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        systemAnalyzerField!.SetValue(systemMetricsService, systemAnalyzerMock.Object);

        var predictiveAnalysisServiceField = typeof(AIOptimizationEngine).GetField("_predictiveAnalysisService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var predictiveAnalysisServiceMock = new Mock<PredictiveAnalysisService>(Mock.Of<ILogger<PredictiveAnalysisService>>());
        predictiveAnalysisServiceMock.Setup(x => x.AnalyzeLoadPatterns())
            .Returns(new LoadPatternData
            {
                Level = LoadLevel.Low,
                Predictions = new List<PredictionResult>(),
                SuccessRate = 0.0,
                AverageImprovement = 0.0,
                TotalPredictions = 0
            });
        predictiveAnalysisServiceField!.SetValue(_engine, predictiveAnalysisServiceMock.Object);

        // Act
        var result = await _engine.GetLoadPatternAnalysisAsync();

        // Assert - Should handle empty predictions gracefully
        Assert.Equal(LoadLevel.Low, result.Level);
        Assert.Empty(result.Predictions);
        Assert.Equal(0, result.TotalPredictions);
        Assert.Equal(0.0, result.SuccessRate);
        Assert.Equal(0.0, result.AverageImprovement);
    }
}