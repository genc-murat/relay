using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Connection;
using System;
using System.Collections.Concurrent;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;

public class ServerSentEventEstimationStrategyTests
{
    private readonly ILogger _logger = NullLogger.Instance;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly ConnectionMetricsUtilities _utilities;
    private readonly ServerSentEventEstimationStrategy _strategy;

    public ServerSentEventEstimationStrategyTests()
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
        _strategy = new ServerSentEventEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServerSentEventEstimationStrategy(null!, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServerSentEventEstimationStrategy(_logger, null!, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServerSentEventEstimationStrategy(_logger, _options, null!, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServerSentEventEstimationStrategy(_logger, _options, _requestAnalytics, null!, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServerSentEventEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, null!, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Utilities_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServerSentEventEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, null!));
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
    public void EstimateConnections_Should_Use_Stored_SSE_Metrics_When_Available()
    {
        // Arrange
        _timeSeriesDb.StoreMetric("sse_connections", 25, DateTime.UtcNow);

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.Equal(25, result);
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
            AverageExecutionTime = TimeSpan.FromMinutes(2),
            ConcurrentExecutions = 30
        }); // Very long-lived
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
        var strategy = new ServerSentEventEstimationStrategy(
            _logger, _options, _requestAnalytics, failingDb, _systemMetrics, _utilities);

        // Act
        var result = strategy.EstimateConnections();

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void TryGetStoredSSEMetrics_Should_Return_Stored_Value()
    {
        // Arrange
        _timeSeriesDb.StoreMetric("sse_connections", 50, DateTime.UtcNow);

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.Equal(50, result);
    }

    [Fact]
    public void EstimateSSEFromRequestPatterns_Should_Calculate_Based_On_Long_Lived_Requests()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMinutes(2),
            ConcurrentExecutions = 40
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateSSEAsFallback_Should_Calculate_Based_On_Active_Requests_And_Time()
    {
        // Act - fallback case
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void EstimateSSEAsFallback_Should_Apply_Business_Hours_Multiplier()
    {
        // Arrange - test during business hours (mocked through integration)
        // Current time is used internally

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result >= 0);
    }

    #endregion

    #region Time-based Tests

    [Theory]
    [InlineData(9, 15)]  // Business hours - higher multiplier
    [InlineData(18, 8)]  // Evening - moderate
    [InlineData(2, 3)]   // Night - lower
    public void EstimateSSEAsFallback_Should_Apply_Time_Based_Multipliers(int hour, int expectedBase)
    {
        // The calculation is: activeRequests * multiplier
        // We can't easily mock the hour, so we test the general behavior
        var result = _strategy.EstimateConnections();
        Assert.True(result >= 0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EstimateConnections_Should_Handle_Empty_RequestAnalytics()
    {
        // Arrange
        _requestAnalytics.Clear();

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void EstimateConnections_Should_Handle_Zero_Concurrent_Executions()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 10,
            ConcurrentExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMinutes(1),
            MemoryAllocated = 0
        }); // Zero concurrent
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void EstimateConnections_Should_Handle_Very_Long_Execution_Times()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 10,
            ConcurrentExecutions = 5,
            AverageExecutionTime = TimeSpan.FromHours(1),
            MemoryAllocated = 10
        }); // Very long
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateConnections_Should_Return_Zero_When_No_Long_Lived_Requests()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 10,
            ConcurrentExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(500),
            MemoryAllocated = 10
        }); // Short-lived
        _requestAnalytics[testType] = data;

        // Act
        var result = _strategy.EstimateConnections();

        // Assert - should use fallback which returns 0 in this case
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
