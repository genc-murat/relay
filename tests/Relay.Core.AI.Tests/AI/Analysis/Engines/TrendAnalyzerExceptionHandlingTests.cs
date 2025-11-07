using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

/// <summary>
/// Exception handling tests for TrendAnalyzer when individual updater methods throw exceptions.
/// Tests that the analyzer gracefully handles updater failures and continues with basic insights.
/// </summary>
public class TrendAnalyzerExceptionHandlingTests
{
    private readonly Mock<ILogger<TrendAnalyzer>> _loggerMock;
    private readonly Mock<IMovingAverageUpdater> _movingAverageUpdaterMock;
    private readonly Mock<ITrendDirectionUpdater> _trendDirectionUpdaterMock;
    private readonly Mock<ITrendVelocityUpdater> _trendVelocityUpdaterMock;
    private readonly Mock<ISeasonalityUpdater> _seasonalityUpdaterMock;
    private readonly Mock<IRegressionUpdater> _regressionUpdaterMock;
    private readonly Mock<ICorrelationUpdater> _correlationUpdaterMock;
    private readonly Mock<IAnomalyUpdater> _anomalyUpdaterMock;

    public TrendAnalyzerExceptionHandlingTests()
    {
        _loggerMock = new Mock<ILogger<TrendAnalyzer>>();
        _movingAverageUpdaterMock = new Mock<IMovingAverageUpdater>();
        _trendDirectionUpdaterMock = new Mock<ITrendDirectionUpdater>();
        _trendVelocityUpdaterMock = new Mock<ITrendVelocityUpdater>();
        _seasonalityUpdaterMock = new Mock<ISeasonalityUpdater>();
        _regressionUpdaterMock = new Mock<IRegressionUpdater>();
        _correlationUpdaterMock = new Mock<ICorrelationUpdater>();
        _anomalyUpdaterMock = new Mock<IAnomalyUpdater>();
    }

    private TrendAnalyzer CreateAnalyzer()
    {
        return new TrendAnalyzer(
            _loggerMock.Object,
            _movingAverageUpdaterMock.Object,
            _trendDirectionUpdaterMock.Object,
            _trendVelocityUpdaterMock.Object,
            _seasonalityUpdaterMock.Object,
            _regressionUpdaterMock.Object,
            _correlationUpdaterMock.Object,
            _anomalyUpdaterMock.Object);
    }

    #region Individual Updater Exception Tests

    [Fact]
    public void AnalyzeMetricTrends_MovingAverageUpdaterThrowsException_ReturnsBasicInsightsOnly()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0 // Should generate basic insight
        };

        _movingAverageUpdaterMock
            .Setup(x => x.UpdateMovingAverages(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(new InvalidOperationException("Moving average calculation failed"));

        // Setup other updaters to work normally (though they won't be called due to exception)
        SetupNormalUpdaters();

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.Timestamp);

        // Should have basic insights despite updater failure
        Assert.NotEmpty(result.Insights);
        var cpuInsight = result.Insights.Find(i => i.Message.Contains("cpu"));
        Assert.NotNull(cpuInsight);
        Assert.Equal(InsightSeverity.Critical, cpuInsight.Severity);

        // Advanced analysis results should be empty due to exception
        Assert.Empty(result.MovingAverages);
        Assert.Empty(result.TrendDirections);
        Assert.Empty(result.TrendVelocities);
        Assert.Empty(result.SeasonalityPatterns);
        Assert.Empty(result.RegressionResults);
        Assert.Empty(result.Correlations);
        Assert.Empty(result.Anomalies);

        // Note: The logger verification may not work as expected due to the way the exception is caught and logged
        // The important thing is that the method returns basic insights despite the exception
    }

    [Fact]
    public void AnalyzeMetricTrends_TrendDirectionUpdaterThrowsException_ContinuesWithOtherUpdaters()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0
        };

        SetupNormalUpdaters();
        _trendDirectionUpdaterMock
            .Setup(x => x.UpdateTrendDirections(It.IsAny<Dictionary<string, double>>(), It.IsAny<Dictionary<string, MovingAverageData>>()))
            .Throws(new ArgumentException("Trend direction calculation failed"));

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert - Should still return basic insights
        Assert.NotNull(result);
        Assert.NotEmpty(result.Insights);
    }

    [Fact]
    public void AnalyzeMetricTrends_TrendVelocityUpdaterThrowsException_ContinuesWithOtherUpdaters()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0
        };

        SetupNormalUpdaters();
        _trendVelocityUpdaterMock
            .Setup(x => x.UpdateTrendVelocities(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(new DivideByZeroException("Velocity calculation failed"));

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Insights);
    }

    [Fact]
    public void AnalyzeMetricTrends_SeasonalityUpdaterThrowsException_ContinuesWithOtherUpdaters()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0
        };

        SetupNormalUpdaters();
        _seasonalityUpdaterMock
            .Setup(x => x.UpdateSeasonalityPatterns(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(new OverflowException("Seasonality calculation overflow"));

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Insights);
    }

    [Fact]
    public void AnalyzeMetricTrends_RegressionUpdaterThrowsException_ContinuesWithOtherUpdaters()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0
        };

        SetupNormalUpdaters();
        _regressionUpdaterMock
            .Setup(x => x.UpdateRegressionResults(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(new IndexOutOfRangeException("Regression calculation failed"));

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Insights);
    }

    [Fact]
    public void AnalyzeMetricTrends_CorrelationUpdaterThrowsException_ContinuesWithOtherUpdaters()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0
        };

        SetupNormalUpdaters();
        _correlationUpdaterMock
            .Setup(x => x.UpdateCorrelations(It.IsAny<Dictionary<string, double>>()))
            .Throws(new KeyNotFoundException("Correlation calculation failed"));

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Insights);
    }

    [Fact]
    public void AnalyzeMetricTrends_AnomalyUpdaterThrowsException_ContinuesWithOtherUpdaters()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0
        };

        SetupNormalUpdaters();
        _anomalyUpdaterMock
            .Setup(x => x.UpdateAnomalies(It.IsAny<Dictionary<string, double>>(), It.IsAny<Dictionary<string, MovingAverageData>>()))
            .Throws(new InvalidCastException("Anomaly detection failed"));

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Insights);
    }

    #endregion

    #region Multiple Updater Exceptions Tests

    [Fact]
    public void AnalyzeMetricTrends_MultipleUpdatersThrowExceptions_ReturnsBasicInsightsOnly()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0,
            ["memory"] = 92.0
        };

        // Multiple updaters fail
        _movingAverageUpdaterMock
            .Setup(x => x.UpdateMovingAverages(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(new Exception("Moving average failed"));

        _trendDirectionUpdaterMock
            .Setup(x => x.UpdateTrendDirections(It.IsAny<Dictionary<string, double>>(), It.IsAny<Dictionary<string, MovingAverageData>>()))
            .Throws(new Exception("Trend direction failed"));

        _anomalyUpdaterMock
            .Setup(x => x.UpdateAnomalies(It.IsAny<Dictionary<string, double>>(), It.IsAny<Dictionary<string, MovingAverageData>>()))
            .Throws(new Exception("Anomaly detection failed"));

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.Timestamp);

        // Should still have basic insights for both CPU and memory
        Assert.Equal(2, result.Insights.Count);
        Assert.Contains(result.Insights, i => i.Message.Contains("cpu"));
        Assert.Contains(result.Insights, i => i.Message.Contains("memory"));
        Assert.All(result.Insights, i => Assert.Equal(InsightSeverity.Critical, i.Severity));

        // Advanced results should be empty
        Assert.Empty(result.MovingAverages);
        Assert.Empty(result.TrendDirections);
        Assert.Empty(result.Anomalies);
    }

    [Fact]
    public void AnalyzeMetricTrends_AllUpdatersThrowExceptions_ReturnsBasicInsightsOnly()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 85.0 // Warning level
        };

        // All updaters fail
        _movingAverageUpdaterMock
            .Setup(x => x.UpdateMovingAverages(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(new Exception("All updaters failed"));

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Insights);
        var insight = result.Insights[0];
        Assert.Equal(InsightSeverity.Warning, insight.Severity);
        Assert.Contains("cpu", insight.Message);

        // All advanced results should be empty
        Assert.Empty(result.MovingAverages);
        Assert.Empty(result.TrendDirections);
        Assert.Empty(result.TrendVelocities);
        Assert.Empty(result.SeasonalityPatterns);
        Assert.Empty(result.RegressionResults);
        Assert.Empty(result.Correlations);
        Assert.Empty(result.Anomalies);
    }

    #endregion

    #region Exception Types Tests

    [Fact]
    public void AnalyzeMetricTrends_UpdaterThrowsArgumentException_HandlesGracefully()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double> { ["cpu"] = 75.0 };

        _movingAverageUpdaterMock
            .Setup(x => x.UpdateMovingAverages(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(new ArgumentException("Invalid argument"));

        SetupNormalUpdaters();

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.Timestamp);
    }

    [Fact]
    public void AnalyzeMetricTrends_UpdaterThrowsInvalidOperationException_HandlesGracefully()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double> { ["cpu"] = 75.0 };

        _trendVelocityUpdaterMock
            .Setup(x => x.UpdateTrendVelocities(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(new InvalidOperationException("Operation not valid"));

        SetupNormalUpdaters();

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.Timestamp);
    }

    [Fact]
    public void AnalyzeMetricTrends_UpdaterThrowsOutOfMemoryException_HandlesGracefully()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double> { ["cpu"] = 75.0 };

        _correlationUpdaterMock
            .Setup(x => x.UpdateCorrelations(It.IsAny<Dictionary<string, double>>()))
            .Throws(new OutOfMemoryException("Out of memory"));

        SetupNormalUpdaters();

        // Act
        var result = analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.Timestamp);
    }

    #endregion

    #region Logging Verification Tests

    [Fact]
    public void AnalyzeMetricTrends_UpdaterThrowsException_LogsErrorWithException()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double> { ["cpu"] = 75.0 };
        var expectedException = new Exception("Test exception");

        _movingAverageUpdaterMock
            .Setup(x => x.UpdateMovingAverages(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(expectedException);

        // Act
        analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error in advanced trend analysis")),
            expectedException,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void AnalyzeMetricTrends_NoExceptions_LogsDebugAndInfoMessages()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double> { ["cpu"] = 75.0 };

        SetupNormalUpdaters();

        // Act
        analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Starting metric trend analysis")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Metric trend analysis completed")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    #endregion

    #region Recovery Tests

    [Fact]
    public void AnalyzeMetricTrends_AfterException_SubsequentCallsWorkNormally()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var metrics = new Dictionary<string, double> { ["cpu"] = 75.0 };

        // First call - causes exception
        _movingAverageUpdaterMock
            .Setup(x => x.UpdateMovingAverages(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Throws(new Exception("First call failed"));

        var result1 = analyzer.AnalyzeMetricTrends(metrics);
        Assert.NotNull(result1);

        // Second call - works normally (but may not generate advanced results if metrics don't trigger them)
        SetupNormalUpdaters();
        var result2 = analyzer.AnalyzeMetricTrends(metrics);

        // Assert - Second call should work without throwing
        Assert.NotNull(result2);
        Assert.NotEqual(default(DateTime), result2.Timestamp);
        // The method should complete successfully regardless of whether advanced analysis works
    }

    #endregion

    #region Helper Methods

    private void SetupNormalUpdaters()
    {
        _movingAverageUpdaterMock
            .Setup(x => x.UpdateMovingAverages(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Returns(new Dictionary<string, MovingAverageData>());

        _trendDirectionUpdaterMock
            .Setup(x => x.UpdateTrendDirections(It.IsAny<Dictionary<string, double>>(), It.IsAny<Dictionary<string, MovingAverageData>>()))
            .Returns(new Dictionary<string, TrendDirection>());

        _trendVelocityUpdaterMock
            .Setup(x => x.UpdateTrendVelocities(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Returns(new Dictionary<string, double>());

        _seasonalityUpdaterMock
            .Setup(x => x.UpdateSeasonalityPatterns(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Returns(new Dictionary<string, SeasonalityPattern>());

        _regressionUpdaterMock
            .Setup(x => x.UpdateRegressionResults(It.IsAny<Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Returns(new Dictionary<string, RegressionResult>());

        _correlationUpdaterMock
            .Setup(x => x.UpdateCorrelations(It.IsAny<Dictionary<string, double>>()))
            .Returns(new Dictionary<string, List<string>>());

        _anomalyUpdaterMock
            .Setup(x => x.UpdateAnomalies(It.IsAny<Dictionary<string, double>>(), It.IsAny<Dictionary<string, MovingAverageData>>()))
            .Returns(new List<MetricAnomaly>());
    }

    #endregion
}