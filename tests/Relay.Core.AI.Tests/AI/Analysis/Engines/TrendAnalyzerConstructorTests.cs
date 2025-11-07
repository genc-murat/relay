using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

/// <summary>
/// Constructor tests for TrendAnalyzer with mocked dependencies.
/// Tests null parameter validation and proper initialization.
/// </summary>
public class TrendAnalyzerConstructorTests
{
    private readonly Mock<ILogger<TrendAnalyzer>> _loggerMock;
    private readonly Mock<IMovingAverageUpdater> _movingAverageUpdaterMock;
    private readonly Mock<ITrendDirectionUpdater> _trendDirectionUpdaterMock;
    private readonly Mock<ITrendVelocityUpdater> _trendVelocityUpdaterMock;
    private readonly Mock<ISeasonalityUpdater> _seasonalityUpdaterMock;
    private readonly Mock<IRegressionUpdater> _regressionUpdaterMock;
    private readonly Mock<ICorrelationUpdater> _correlationUpdaterMock;
    private readonly Mock<IAnomalyUpdater> _anomalyUpdaterMock;

    public TrendAnalyzerConstructorTests()
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

    #region Valid Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var analyzer = new TrendAnalyzer(
            _loggerMock.Object,
            _movingAverageUpdaterMock.Object,
            _trendDirectionUpdaterMock.Object,
            _trendVelocityUpdaterMock.Object,
            _seasonalityUpdaterMock.Object,
            _regressionUpdaterMock.Object,
            _correlationUpdaterMock.Object,
            _anomalyUpdaterMock.Object);

        // Assert
        Assert.NotNull(analyzer);
    }

    [Fact]
    public void Constructor_AssignsDependenciesCorrectly()
    {
        // Act
        var analyzer = new TrendAnalyzer(
            _loggerMock.Object,
            _movingAverageUpdaterMock.Object,
            _trendDirectionUpdaterMock.Object,
            _trendVelocityUpdaterMock.Object,
            _seasonalityUpdaterMock.Object,
            _regressionUpdaterMock.Object,
            _correlationUpdaterMock.Object,
            _anomalyUpdaterMock.Object);

        // Assert - We can't directly test private fields, but we can verify the instance works
        Assert.NotNull(analyzer);
        // The analyzer should be functional with the provided mocks
        var result = analyzer.AnalyzeMetricTrends(new System.Collections.Generic.Dictionary<string, double>());
        Assert.NotNull(result);
    }

    #endregion

    #region Null Parameter Validation Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TrendAnalyzer(
                null!, // logger
                _movingAverageUpdaterMock.Object,
                _trendDirectionUpdaterMock.Object,
                _trendVelocityUpdaterMock.Object,
                _seasonalityUpdaterMock.Object,
                _regressionUpdaterMock.Object,
                _correlationUpdaterMock.Object,
                _anomalyUpdaterMock.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullMovingAverageUpdater_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TrendAnalyzer(
                _loggerMock.Object,
                null!, // movingAverageUpdater
                _trendDirectionUpdaterMock.Object,
                _trendVelocityUpdaterMock.Object,
                _seasonalityUpdaterMock.Object,
                _regressionUpdaterMock.Object,
                _correlationUpdaterMock.Object,
                _anomalyUpdaterMock.Object));

        Assert.Equal("movingAverageUpdater", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTrendDirectionUpdater_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TrendAnalyzer(
                _loggerMock.Object,
                _movingAverageUpdaterMock.Object,
                null!, // trendDirectionUpdater
                _trendVelocityUpdaterMock.Object,
                _seasonalityUpdaterMock.Object,
                _regressionUpdaterMock.Object,
                _correlationUpdaterMock.Object,
                _anomalyUpdaterMock.Object));

        Assert.Equal("trendDirectionUpdater", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTrendVelocityUpdater_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TrendAnalyzer(
                _loggerMock.Object,
                _movingAverageUpdaterMock.Object,
                _trendDirectionUpdaterMock.Object,
                null!, // trendVelocityUpdater
                _seasonalityUpdaterMock.Object,
                _regressionUpdaterMock.Object,
                _correlationUpdaterMock.Object,
                _anomalyUpdaterMock.Object));

        Assert.Equal("trendVelocityUpdater", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSeasonalityUpdater_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TrendAnalyzer(
                _loggerMock.Object,
                _movingAverageUpdaterMock.Object,
                _trendDirectionUpdaterMock.Object,
                _trendVelocityUpdaterMock.Object,
                null!, // seasonalityUpdater
                _regressionUpdaterMock.Object,
                _correlationUpdaterMock.Object,
                _anomalyUpdaterMock.Object));

        Assert.Equal("seasonalityUpdater", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRegressionUpdater_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TrendAnalyzer(
                _loggerMock.Object,
                _movingAverageUpdaterMock.Object,
                _trendDirectionUpdaterMock.Object,
                _trendVelocityUpdaterMock.Object,
                _seasonalityUpdaterMock.Object,
                null!, // regressionUpdater
                _correlationUpdaterMock.Object,
                _anomalyUpdaterMock.Object));

        Assert.Equal("regressionUpdater", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCorrelationUpdater_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TrendAnalyzer(
                _loggerMock.Object,
                _movingAverageUpdaterMock.Object,
                _trendDirectionUpdaterMock.Object,
                _trendVelocityUpdaterMock.Object,
                _seasonalityUpdaterMock.Object,
                _regressionUpdaterMock.Object,
                null!, // correlationUpdater
                _anomalyUpdaterMock.Object));

        Assert.Equal("correlationUpdater", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullAnomalyUpdater_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TrendAnalyzer(
                _loggerMock.Object,
                _movingAverageUpdaterMock.Object,
                _trendDirectionUpdaterMock.Object,
                _trendVelocityUpdaterMock.Object,
                _seasonalityUpdaterMock.Object,
                _regressionUpdaterMock.Object,
                _correlationUpdaterMock.Object,
                null!)); // anomalyUpdater

        Assert.Equal("anomalyUpdater", exception.ParamName);
    }

    #endregion

    #region Multiple Null Parameters Tests

    [Fact]
    public void Constructor_WithMultipleNullParameters_ThrowsForFirstNullParameter()
    {
        // Act & Assert - Should throw for the first null parameter (logger)
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TrendAnalyzer(
                null!, // logger - first null
                null!, // movingAverageUpdater - second null
                _trendDirectionUpdaterMock.Object,
                _trendVelocityUpdaterMock.Object,
                _seasonalityUpdaterMock.Object,
                _regressionUpdaterMock.Object,
                _correlationUpdaterMock.Object,
                _anomalyUpdaterMock.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region Dependency Usage Validation Tests

    [Fact]
    public void AnalyzeMetricTrends_UsesMovingAverageUpdater()
    {
        // Arrange
        var analyzer = new TrendAnalyzer(
            _loggerMock.Object,
            _movingAverageUpdaterMock.Object,
            _trendDirectionUpdaterMock.Object,
            _trendVelocityUpdaterMock.Object,
            _seasonalityUpdaterMock.Object,
            _regressionUpdaterMock.Object,
            _correlationUpdaterMock.Object,
            _anomalyUpdaterMock.Object);

        var metrics = new System.Collections.Generic.Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };

        _movingAverageUpdaterMock.Setup(x => x.UpdateMovingAverages(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Returns(new System.Collections.Generic.Dictionary<string, MovingAverageData>());

        // Act
        analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        _movingAverageUpdaterMock.Verify(x => x.UpdateMovingAverages(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public void AnalyzeMetricTrends_UsesAllUpdaters()
    {
        // Arrange
        var analyzer = new TrendAnalyzer(
            _loggerMock.Object,
            _movingAverageUpdaterMock.Object,
            _trendDirectionUpdaterMock.Object,
            _trendVelocityUpdaterMock.Object,
            _seasonalityUpdaterMock.Object,
            _regressionUpdaterMock.Object,
            _correlationUpdaterMock.Object,
            _anomalyUpdaterMock.Object);

        var metrics = new System.Collections.Generic.Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };

        // Setup all mocks to return empty collections
        _movingAverageUpdaterMock.Setup(x => x.UpdateMovingAverages(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Returns(new System.Collections.Generic.Dictionary<string, MovingAverageData>());
        _trendDirectionUpdaterMock.Setup(x => x.UpdateTrendDirections(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<System.Collections.Generic.Dictionary<string, MovingAverageData>>()))
            .Returns(new System.Collections.Generic.Dictionary<string, TrendDirection>());
        _trendVelocityUpdaterMock.Setup(x => x.UpdateTrendVelocities(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Returns(new System.Collections.Generic.Dictionary<string, double>());
        _seasonalityUpdaterMock.Setup(x => x.UpdateSeasonalityPatterns(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Returns(new System.Collections.Generic.Dictionary<string, SeasonalityPattern>());
        _regressionUpdaterMock.Setup(x => x.UpdateRegressionResults(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()))
            .Returns(new System.Collections.Generic.Dictionary<string, RegressionResult>());
        _correlationUpdaterMock.Setup(x => x.UpdateCorrelations(It.IsAny<System.Collections.Generic.Dictionary<string, double>>()))
            .Returns(new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>());
        _anomalyUpdaterMock.Setup(x => x.UpdateAnomalies(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<System.Collections.Generic.Dictionary<string, MovingAverageData>>()))
            .Returns(new System.Collections.Generic.List<MetricAnomaly>());

        // Act
        analyzer.AnalyzeMetricTrends(metrics);

        // Assert - All updaters should be called
        _movingAverageUpdaterMock.Verify(x => x.UpdateMovingAverages(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()), Times.Once);
        _trendDirectionUpdaterMock.Verify(x => x.UpdateTrendDirections(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<System.Collections.Generic.Dictionary<string, MovingAverageData>>()), Times.Once);
        _trendVelocityUpdaterMock.Verify(x => x.UpdateTrendVelocities(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()), Times.Once);
        _seasonalityUpdaterMock.Verify(x => x.UpdateSeasonalityPatterns(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()), Times.Once);
        _regressionUpdaterMock.Verify(x => x.UpdateRegressionResults(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<DateTime>()), Times.Once);
        _correlationUpdaterMock.Verify(x => x.UpdateCorrelations(It.IsAny<System.Collections.Generic.Dictionary<string, double>>()), Times.Once);
        _anomalyUpdaterMock.Verify(x => x.UpdateAnomalies(It.IsAny<System.Collections.Generic.Dictionary<string, double>>(), It.IsAny<System.Collections.Generic.Dictionary<string, MovingAverageData>>()), Times.Once);
    }

    #endregion
}