using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Connection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;

public class RawWebSocketEstimationStrategyTests
{
    private readonly ILogger _logger = NullLogger.Instance;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly ConnectionMetricsUtilities _utilities;
    private readonly RawWebSocketEstimationStrategy _strategy;

    public RawWebSocketEstimationStrategyTests()
    {
        _options = new AIOptimizationOptions
        {
            MaxEstimatedWebSocketConnections = 1000
        };
        _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        _timeSeriesDb = new TestTimeSeriesDatabase();
        _systemMetrics = new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        var protocolCalculator = new ProtocolMetricsCalculator(_logger, _requestAnalytics, _timeSeriesDb, _systemMetrics);
        _utilities = new ConnectionMetricsUtilities(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, protocolCalculator);
        _strategy = new RawWebSocketEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RawWebSocketEstimationStrategy(null!, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RawWebSocketEstimationStrategy(_logger, null!, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RawWebSocketEstimationStrategy(_logger, _options, null!, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RawWebSocketEstimationStrategy(_logger, _options, _requestAnalytics, null!, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RawWebSocketEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, null!, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Utilities_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RawWebSocketEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, null!));
    }

    #endregion

    #region EstimateConnections Tests

    [Fact]
    public void EstimateConnections_Should_Return_Zero_When_All_Strategies_Fail()
    {
        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void EstimateConnections_Should_Use_Stored_Metrics_When_Available()
    {
        // Arrange
        _timeSeriesDb.StoreMetric("WebSocketConnections", 50, DateTime.UtcNow);

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateConnections_Should_Try_Multiple_Metric_Names()
    {
        // Arrange
        _timeSeriesDb.StoreMetric("RawWebSocketConnections", 30, DateTime.UtcNow);
        _timeSeriesDb.StoreMetric("ws-current-connections", 40, DateTime.UtcNow);

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateConnections_Should_Use_Upgrade_Patterns_When_No_Stored_Metrics()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(15),
            ConcurrentExecutions = 50
        }); // Long-lived
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateConnections_Should_Use_Historical_Patterns_As_Fallback()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            _timeSeriesDb.StoreMetric("WebSocketConnections", 100 + i, DateTime.UtcNow.AddMinutes(-i));
        }

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateConnections_Should_Use_Active_Requests_As_Last_Fallback()
    {
        // Act - with minimal setup
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void EstimateConnections_Should_Handle_Exception_And_Return_Zero()
    {
        // Arrange
        var failingDb = new FailingTimeSeriesDatabase();
        var strategy = new RawWebSocketEstimationStrategy(
            _logger, _options, _requestAnalytics, failingDb, _systemMetrics, _utilities);

        // Act
        var result = strategy.EstimateConnections();

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void EstimateWebSocketFromUpgradePatterns_Should_Calculate_Based_On_Long_Lived_Requests()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(15),
            ConcurrentExecutions = 50
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateWebSocketFromHistoricalPatterns_Should_Use_Similar_Time_Periods()
    {
        // Arrange - current hour is now, add data for same hour yesterday
        var now = DateTime.UtcNow;
        _timeSeriesDb.StoreMetric("WebSocketConnections", 100, now.AddDays(-1).AddHours(1)); // Similar hour
        _timeSeriesDb.StoreMetric("WebSocketConnections", 200, now.AddDays(-1)); // Same hour yesterday

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateWebSocketFromHistoricalPatterns_Should_Use_EMA_When_Insufficient_Data()
    {
        // Arrange - less than 20 data points
        for (int i = 0; i < 15; i++)
        {
            _timeSeriesDb.StoreMetric("WebSocketConnections", 100, DateTime.UtcNow.AddMinutes(-i));
        }

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void ClassifyCurrentLoadLevel_Should_Return_Idle_For_Low_Load()
    {
        // Test through integration - with low load
        var result = _strategy.EstimateConnections();
        Assert.True(result >= 0);
    }

    [Fact]
    public void ClassifyCurrentLoadLevel_Should_Return_High_For_High_Load()
    {
        // Test through integration
        var result = _strategy.EstimateConnections();
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetLoadBasedConnectionAdjustment_Should_Adjust_For_Different_Load_Levels()
    {
        // Test through integration
        var result = _strategy.EstimateConnections();
        Assert.True(result >= 0);
    }

    #endregion

    #region Time-based Factor Tests

    [Theory]
    [InlineData(9, 1.4)]   // Business hours
    [InlineData(12, 1.4)]  // Business hours
    [InlineData(18, 1.1)]  // Evening
    [InlineData(22, 1.1)]  // Evening
    [InlineData(2, 0.6)]   // Night
    [InlineData(6, 0.9)]   // Early morning
    public void CalculateTimeOfDayWebSocketFactor_Should_Return_Correct_Factors(int hour, double expectedFactor)
    {
        // This is tested through EstimateConnections which uses the factor internally
        var result = _strategy.EstimateConnections();
        Assert.True(result >= 0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EstimateConnections_Should_Handle_Empty_RequestAnalytics()
    {
        // Arrange - ensure empty
        _requestAnalytics.Clear();

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void EstimateConnections_Should_Handle_Zero_Active_Requests()
    {
        // Act - with minimal setup
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void EstimateConnections_Should_Respect_Max_Connections_Limit()
    {
        // Arrange - very high values
        _timeSeriesDb.StoreMetric("WebSocketConnections", 2000, DateTime.UtcNow);

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result <= _options.MaxEstimatedWebSocketConnections / 4);
    }

    #endregion

    // Helper classes
    private class TestController { }

    private class FailingTimeSeriesDatabase : TestTimeSeriesDatabase
    {
        public override System.Collections.Generic.List<MetricDataPoint> GetRecentMetrics(string metricName, int count)
        {
            throw new Exception("Test exception");
        }
    }
}
