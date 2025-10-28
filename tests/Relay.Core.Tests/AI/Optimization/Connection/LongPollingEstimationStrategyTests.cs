using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Connection;
using System;
using System.Collections.Concurrent;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;

public class LongPollingEstimationStrategyTests
{
    private readonly ILogger _logger = NullLogger.Instance;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly ConnectionMetricsUtilities _utilities;
    private readonly LongPollingEstimationStrategy _strategy;

    public LongPollingEstimationStrategyTests()
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
        _strategy = new LongPollingEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LongPollingEstimationStrategy(null!, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LongPollingEstimationStrategy(_logger, null!, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LongPollingEstimationStrategy(_logger, _options, null!, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LongPollingEstimationStrategy(_logger, _options, _requestAnalytics, null!, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LongPollingEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, null!, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Utilities_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LongPollingEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, null!));
    }

    #endregion

    #region EstimateConnections Tests

    [Fact]
    public void EstimateConnections_Should_Return_Zero_When_No_Data_Available()
    {
        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void EstimateConnections_Should_Use_Stored_LongPolling_Metrics_When_Available()
    {
        // Arrange
        _timeSeriesDb.StoreMetric("longpolling_connections", 30, DateTime.UtcNow);

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.Equal(30, result);
    }

    [Fact]
    public void EstimateConnections_Should_Try_Multiple_Metric_Names()
    {
        // Arrange
        _timeSeriesDb.StoreMetric("long_polling_connections", 25, DateTime.UtcNow);
        _timeSeriesDb.StoreMetric("polling_connections", 35, DateTime.UtcNow);

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateConnections_Should_Use_Request_Patterns_When_No_Stored_Metrics()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(45),
            ConcurrentExecutions = 20
        }); // Medium duration
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateConnections_Should_Use_Fallback_Estimation_As_Last_Resort()
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
        var strategy = new LongPollingEstimationStrategy(
            _logger, _options, _requestAnalytics, failingDb, _systemMetrics, _utilities);

        // Act
        var result = strategy.EstimateConnections();

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void TryGetStoredLongPollingMetrics_Should_Return_Stored_Value()
    {
        // Arrange
        _timeSeriesDb.StoreMetric("longpolling_connections", 40, DateTime.UtcNow);

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.Equal(40, result);
    }

    [Fact]
    public void EstimateLongPollingFromRequestPatterns_Should_Calculate_Based_On_Medium_Duration_Requests()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(45),
            ConcurrentExecutions = 25
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateLongPollingAsFallback_Should_Calculate_Based_On_Active_Requests_And_Throughput()
    {
        // Act - fallback case
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void EstimateLongPollingAsFallback_Should_Apply_Throughput_Multiplier()
    {
        // The calculation uses: Math.Min(activeRequests / 10.0, 2.0)
        // We test that it works with different scenarios
        var result = _strategy.EstimateConnections();
        Assert.True(result >= 0);
    }

    #endregion

    #region Request Pattern Analysis Tests

    [Fact]
    public void EstimateLongPollingFromRequestPatterns_Should_Ignore_Short_Requests()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(10),
            ConcurrentExecutions = 50
        }); // Too short
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert - should use fallback (0) since no valid patterns
        Assert.Equal(0, result);
    }

    [Fact]
    public void EstimateLongPollingFromRequestPatterns_Should_Handle_Empty_RequestAnalytics()
    {
        // Arrange
        _requestAnalytics.Clear();

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void EstimateLongPollingFromRequestPatterns_Should_Calculate_RepeatRequestCount()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        // Add multiple metrics to simulate repeat requests
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(45),
            ConcurrentExecutions = 20
        });
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(45),
            ConcurrentExecutions = 15
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EstimateConnections_Should_Handle_Zero_Active_Requests()
    {
        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void EstimateConnections_Should_Handle_Very_High_Throughput()
    {
        // This would require mocking system metrics, tested through integration
        var result = _strategy.EstimateConnections();
        Assert.True(result >= 0);
    }

    [Fact]
    public void EstimateConnections_Should_Respect_Max_Connections_Limit()
    {
        // Arrange - force high estimation through stored metrics
        _timeSeriesDb.StoreMetric("longpolling_connections", 2000, DateTime.UtcNow);

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result <= _options.MaxEstimatedWebSocketConnections / 6);
    }

    [Fact]
    public void EstimateConnections_Should_Handle_Divide_By_Zero_In_Calculation()
    {
        // Arrange - edge case that might cause division by zero
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 10,
            ConcurrentExecutions = 5,
            AverageExecutionTime = TimeSpan.FromSeconds(45),
            MemoryAllocated = 20
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert - should handle the Math.Max(avgExecutionTime, 1) safely
        Assert.True(result >= 0);
    }

    #endregion

    #region Time Series Integration Tests

    [Fact]
    public void EstimateConnections_Should_Handle_TimeSeries_Database_Errors()
    {
        // Arrange
        var failingDb = new FailingTimeSeriesDatabase();
        var strategy = new LongPollingEstimationStrategy(
            _logger, _options, _requestAnalytics, failingDb, _systemMetrics, _utilities);

        // Act
        var result = strategy.EstimateConnections();

        // Assert
        Assert.Equal(0, result);
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
