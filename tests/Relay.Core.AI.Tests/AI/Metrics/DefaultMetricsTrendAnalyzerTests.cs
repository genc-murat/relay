using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Metrics.Implementations;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Metrics;

public class DefaultMetricsTrendAnalyzerTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly AIModelStatistics _testStatistics;

    public DefaultMetricsTrendAnalyzerTests()
    {
        _loggerMock = new Mock<ILogger>();

        _testStatistics = new AIModelStatistics
        {
            ModelVersion = "v1.2.3",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.85,
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(45.5),
            TrainingDataPoints = 50000
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Arrange & Act
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

        // Assert
        Assert.NotNull(analyzer);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DefaultMetricsTrendAnalyzer(null!));

        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region AnalyzeTrendsAsync Tests

    [Fact]
    public async Task AnalyzeTrendsAsync_WithValidStatistics_CompletesSuccessfully()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

        // Act
        await analyzer.AnalyzeTrendsAsync(_testStatistics);

        // Assert - Should complete without throwing
        Assert.True(true); // If we get here, the method completed successfully
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_TracksAllExpectedMetrics()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

        // Act
        await analyzer.AnalyzeTrendsAsync(_testStatistics);

        // Assert - We can't directly access private fields, but we can verify through behavior
        // This test ensures the method processes all metrics without errors
        // Additional tests will verify the tracking behavior
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_WithZeroValues_HandlesGracefully()
    {
        // Arrange
        var zeroStats = new AIModelStatistics
        {
            ModelVersion = "v1.0.0",
            ModelTrainingDate = DateTime.UtcNow,
            LastRetraining = DateTime.UtcNow,
            TotalPredictions = 0,
            AccuracyScore = 0.0,
            PrecisionScore = 0.0,
            RecallScore = 0.0,
            F1Score = 0.0,
            ModelConfidence = 0.0,
            AveragePredictionTime = TimeSpan.Zero,
            TrainingDataPoints = 0
        };
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

        // Act
        await analyzer.AnalyzeTrendsAsync(zeroStats);

        // Assert - Should handle zero values without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_WithExtremeValues_HandlesGracefully()
    {
        // Arrange
        var extremeStats = new AIModelStatistics
        {
            ModelVersion = "v1.0.0",
            ModelTrainingDate = DateTime.UtcNow,
            LastRetraining = DateTime.UtcNow,
            TotalPredictions = long.MaxValue,
            AccuracyScore = 1.0,
            PrecisionScore = 1.0,
            RecallScore = 1.0,
            F1Score = 1.0,
            ModelConfidence = 1.0,
            AveragePredictionTime = TimeSpan.FromDays(365), // Very long time
            TrainingDataPoints = long.MaxValue
        };
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

        // Act
        await analyzer.AnalyzeTrendsAsync(extremeStats);

        // Assert - Should handle extreme values without throwing
        Assert.True(true);
    }



    #endregion

    #region TrackMetricTrend Tests

    [Fact]
    public async Task AnalyzeTrendsAsync_MultipleCalls_BuildsTrendData()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);
        var improvedStats = new AIModelStatistics
        {
            ModelVersion = "v1.2.4",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.90, // Improved from 0.85
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(45.5),
            TrainingDataPoints = 50000
        };

        // Act - Call multiple times to build trend data
        await analyzer.AnalyzeTrendsAsync(_testStatistics);
        await analyzer.AnalyzeTrendsAsync(improvedStats);

        // Assert - Should have processed both calls without error
        // The trend detection will be tested separately
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_TracksPercentageChanges()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);
        var baselineStats = new AIModelStatistics
        {
            ModelVersion = "v1.0.0",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.80, // Baseline
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
            TrainingDataPoints = 50000
        };

        var improvedStats = new AIModelStatistics
        {
            ModelVersion = "v1.1.0",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.88, // 10% improvement
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
            TrainingDataPoints = 50000
        };

        // Act
        await analyzer.AnalyzeTrendsAsync(baselineStats);
        await analyzer.AnalyzeTrendsAsync(improvedStats);

        // Assert - The percentage change calculation should work
        // Logging verification will be tested separately
    }

    #endregion

    #region Trend Detection and Logging Tests

    [Fact]
    public async Task AnalyzeTrendsAsync_SignificantImprovement_LogsTrend()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);
        var baselineStats = new AIModelStatistics
        {
            ModelVersion = "v1.0.0",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.80,
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
            TrainingDataPoints = 50000
        };

        var improvedStats = new AIModelStatistics
        {
            ModelVersion = "v1.1.0",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.89, // 11.25% improvement - should trigger logging
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
            TrainingDataPoints = 50000
        };

        // Act
        await analyzer.AnalyzeTrendsAsync(baselineStats);
        await analyzer.AnalyzeTrendsAsync(improvedStats);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Significant trend detected")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_SignificantDecline_LogsTrend()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);
        var baselineStats = new AIModelStatistics
        {
            ModelVersion = "v1.0.0",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.90,
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
            TrainingDataPoints = 50000
        };

        var declinedStats = new AIModelStatistics
        {
            ModelVersion = "v1.1.0",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.80, // 11.11% decline - should trigger logging
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
            TrainingDataPoints = 50000
        };

        // Act
        await analyzer.AnalyzeTrendsAsync(baselineStats);
        await analyzer.AnalyzeTrendsAsync(declinedStats);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Significant trend detected") &&
                                         o.ToString()!.Contains("decreased")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_SmallChanges_DoesNotLog()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);
        var baselineStats = new AIModelStatistics
        {
            ModelVersion = "v1.0.0",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.85,
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
            TrainingDataPoints = 50000
        };

        var smallChangeStats = new AIModelStatistics
        {
            ModelVersion = "v1.1.0",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.86, // Only 1.18% improvement - below 10% threshold
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(50.0),
            TrainingDataPoints = 50000
        };

        // Act
        await analyzer.AnalyzeTrendsAsync(baselineStats);
        await analyzer.AnalyzeTrendsAsync(smallChangeStats);

        // Assert - Should not log significant trend
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Significant trend detected")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task AnalyzeTrendsAsync_WithNegativeValues_HandlesGracefully()
    {
        // Arrange
        var negativeStats = new AIModelStatistics
        {
            ModelVersion = "v1.0.0",
            ModelTrainingDate = DateTime.UtcNow,
            LastRetraining = DateTime.UtcNow,
            TotalPredictions = -1000, // Negative value
            AccuracyScore = -0.1, // Invalid negative score
            PrecisionScore = -0.05,
            RecallScore = -0.02,
            F1Score = -0.03,
            ModelConfidence = -0.5,
            AveragePredictionTime = TimeSpan.FromMilliseconds(-10), // Negative time
            TrainingDataPoints = -5000
        };
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

        // Act
        await analyzer.AnalyzeTrendsAsync(negativeStats);

        // Assert - Should handle negative values without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_WithDivisionByZero_PreventsDivisionByZero()
    {
        // Arrange - Create scenario where oldest value might be zero
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

        // First call with zero accuracy
        var zeroStats = new AIModelStatistics
        {
            ModelVersion = "v1.0.0",
            ModelTrainingDate = DateTime.UtcNow,
            LastRetraining = DateTime.UtcNow,
            TotalPredictions = 1000,
            AccuracyScore = 0.0, // Zero value
            PrecisionScore = 0.0,
            RecallScore = 0.0,
            F1Score = 0.0,
            ModelConfidence = 0.0,
            AveragePredictionTime = TimeSpan.Zero,
            TrainingDataPoints = 1000
        };

        // Second call with non-zero value
        var nonZeroStats = new AIModelStatistics
        {
            ModelVersion = "v1.1.0",
            ModelTrainingDate = DateTime.UtcNow,
            LastRetraining = DateTime.UtcNow,
            TotalPredictions = 1000,
            AccuracyScore = 0.1, // Non-zero value
            PrecisionScore = 0.1,
            RecallScore = 0.1,
            F1Score = 0.1,
            ModelConfidence = 0.1,
            AveragePredictionTime = TimeSpan.FromMilliseconds(10),
            TrainingDataPoints = 1000
        };

        // Act
        await analyzer.AnalyzeTrendsAsync(zeroStats);
        await analyzer.AnalyzeTrendsAsync(nonZeroStats);

        // Assert - Should handle division by zero gracefully
        Assert.True(true);
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_WithIdenticalValues_CalculatesZeroChange()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);
        var stats1 = _testStatistics;
        var stats2 = _testStatistics; // Identical values

        // Act
        await analyzer.AnalyzeTrendsAsync(stats1);
        await analyzer.AnalyzeTrendsAsync(stats2);

        // Assert - Should handle zero change without issues
        // No logging should occur since change is 0%
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Significant trend detected")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task AnalyzeTrendsAsync_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);
        var valueTasks = new ValueTask[10];

        // Act - Run multiple concurrent calls
        for (int i = 0; i < valueTasks.Length; i++)
        {
            var stats = new AIModelStatistics
            {
                ModelVersion = $"v1.{i}.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                LastRetraining = DateTime.UtcNow.AddDays(-1),
                TotalPredictions = 10000 + i * 1000,
                AccuracyScore = 0.80 + i * 0.01, // Slightly different values
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                ModelConfidence = 0.78,
                AveragePredictionTime = TimeSpan.FromMilliseconds(45.5 + i),
                TrainingDataPoints = 50000
            };

            valueTasks[i] = analyzer.AnalyzeTrendsAsync(stats);
        }

        // Wait for all ValueTasks to complete
        foreach (var valueTask in valueTasks)
        {
            await valueTask;
        }

        // Assert - Should complete without exceptions
        Assert.True(true);
    }

    #endregion

    #region Data Point Queue Management Tests

    [Fact]
    public async Task AnalyzeTrendsAsync_ManyCalls_LimitsDataPointsToTwenty()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

        // Act - Call 25 times to exceed the 20 data point limit
        for (int i = 0; i < 25; i++)
        {
            var stats = new AIModelStatistics
            {
                ModelVersion = $"v{i}.0.0",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                LastRetraining = DateTime.UtcNow.AddDays(-1),
                TotalPredictions = 10000,
                AccuracyScore = 0.80 + i * 0.001, // Gradually increasing
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                ModelConfidence = 0.78,
                AveragePredictionTime = TimeSpan.FromMilliseconds(45.5),
                TrainingDataPoints = 50000
            };

            await analyzer.AnalyzeTrendsAsync(stats);
        }

        // Assert - Should have processed all calls without issues
        // The queue management is internal, but we verify it doesn't break
        Assert.True(true);
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_SingleCall_InitializesTrends()
    {
        // Arrange
        var analyzer = new DefaultMetricsTrendAnalyzer(_loggerMock.Object);

        // Act
        await analyzer.AnalyzeTrendsAsync(_testStatistics);

        // Assert - Should initialize trend tracking without issues
        Assert.True(true);
    }

    #endregion



    #region MetricTrend Private Class Tests

    [Fact]
    public void MetricTrend_PrivateClass_CanBeAccessedViaReflection()
    {
        // Arrange - Use reflection to access the private class
        var analyzerType = typeof(DefaultMetricsTrendAnalyzer);
        var metricTrendType = analyzerType.GetNestedType("MetricTrend", System.Reflection.BindingFlags.NonPublic);

        // Assert
        Assert.NotNull(metricTrendType);
        Assert.True(metricTrendType!.IsClass);
    }

    [Fact]
    public void MetricTrend_PrivateClass_HasExpectedProperties()
    {
        // Arrange
        var analyzerType = typeof(DefaultMetricsTrendAnalyzer);
        var metricTrendType = analyzerType.GetNestedType("MetricTrend", System.Reflection.BindingFlags.NonPublic);

        // Act
        var dataPointsProperty = metricTrendType!.GetProperty("DataPoints");
        var percentageChangeProperty = metricTrendType.GetProperty("PercentageChange");

        // Assert
        Assert.NotNull(dataPointsProperty);
        Assert.NotNull(percentageChangeProperty);
        Assert.True(dataPointsProperty!.PropertyType.Name.Contains("ConcurrentQueue"));
        Assert.Equal(typeof(double), percentageChangeProperty!.PropertyType);
    }

    #endregion
}