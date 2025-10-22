using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Optimization.Connection;
using System;
using System.Collections.Concurrent;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;


public class ConnectionMetricsProviderTests
{
    private readonly ILogger _logger;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly ConnectionMetricsCollector _connectionMetrics;
    private readonly ConnectionMetricsProvider _provider;

    public ConnectionMetricsProviderTests()
    {
        _logger = NullLogger.Instance;
        _options = new AIOptimizationOptions
        {
            MaxEstimatedHttpConnections = 1000,
            MaxEstimatedDbConnections = 50,
            EstimatedMaxDbConnections = 100,
            MaxEstimatedExternalConnections = 30,
            MaxEstimatedWebSocketConnections = 1000
        };
        _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        _timeSeriesDb = new TestTimeSeriesDatabase();
        _systemMetrics = new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        _connectionMetrics = new ConnectionMetricsCollector(NullLogger<ConnectionMetricsCollector>.Instance, _options, _requestAnalytics);

        _provider = new ConnectionMetricsProvider(
            _logger,
            _options,
            _requestAnalytics,
            _timeSeriesDb,
            _systemMetrics,
            _connectionMetrics);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsProvider(
                null!,
                _options,
                _requestAnalytics,
                _timeSeriesDb,
                _systemMetrics,
                _connectionMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsProvider(
                _logger,
                null!,
                _requestAnalytics,
                _timeSeriesDb,
                _systemMetrics,
                _connectionMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsProvider(
                _logger,
                _options,
                null!,
                _timeSeriesDb,
                _systemMetrics,
                _connectionMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsProvider(
                _logger,
                _options,
                _requestAnalytics,
                null!,
                _systemMetrics,
                _connectionMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsProvider(
                _logger,
                _options,
                _requestAnalytics,
                _timeSeriesDb,
                null!,
                _connectionMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_ConnectionMetrics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsProvider(
                _logger,
                _options,
                _requestAnalytics,
                _timeSeriesDb,
                _systemMetrics,
                null!));
    }

    #endregion

    #region GetActiveConnectionCount Tests

    [Fact]
    public void GetActiveConnectionCount_Should_Return_Valid_Count()
    {
        // Act
        var result = _provider.GetActiveConnectionCount();

        // Assert
        Assert.True(result >= 0);
    }



    #endregion

    #region GetHttpConnectionCount Tests

    [Fact]
    public void GetHttpConnectionCount_Should_Return_Valid_Count()
    {
        // Act
        var result = _provider.GetHttpConnectionCount();

        // Assert
        Assert.True(result >= 0);
    }

    #endregion

    #region GetWebSocketConnectionCount Tests

    [Fact]
    public void GetWebSocketConnectionCount_Should_Return_Valid_Count()
    {
        // Act
        var result = _provider.GetWebSocketConnectionCount();

        // Assert
        Assert.True(result >= 0);
    }

    #endregion

    #region GetDatabaseConnectionCount Tests

    [Fact]
    public void GetDatabaseConnectionCount_Should_Return_Valid_Count()
    {
        // Act
        var result = _provider.GetDatabaseConnectionCount();

        // Assert
        Assert.True(result >= 0);
    }

    #endregion

    #region GetExternalServiceConnectionCount Tests

    [Fact]
    public void GetExternalServiceConnectionCount_Should_Return_Valid_Count()
    {
        // Act
        var result = _provider.GetExternalServiceConnectionCount();

        // Assert
        Assert.True(result >= 0);
    }

    #endregion

    #region GetConnectionHealthScore Tests

    [Fact]
    public void GetConnectionHealthScore_Should_Return_Value_Between_Zero_And_One()
    {
        // Act
        var result = _provider.GetConnectionHealthScore();

        // Assert
        Assert.True(result >= 0.0);
        Assert.True(result <= 1.0);
    }

    #endregion

    #region GetConnectionLoadFactor Tests

    [Fact]
    public void GetConnectionLoadFactor_Should_Return_Value_Between_Zero_And_One()
    {
        // Act
        var result = _provider.GetConnectionLoadFactor();

        // Assert
        Assert.True(result >= 0.0);
        Assert.True(result <= 1.0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void All_Methods_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                // Test various methods concurrently
                var totalCount = _provider.GetActiveConnectionCount();
                var httpCount = _provider.GetHttpConnectionCount();
                var wsCount = _provider.GetWebSocketConnectionCount();
                var healthScore = _provider.GetConnectionHealthScore();

                Assert.True(totalCount >= 0);
                Assert.True(httpCount >= 0);
                Assert.True(wsCount >= 0);
                Assert.True(healthScore >= 0.0 && healthScore <= 1.0);
            });
        }

        // Act & Assert
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    #endregion

    #region GetAspNetCoreConnectionCountLegacy Tests

    [Fact]
    public void GetAspNetCoreConnectionCountLegacy_Should_Return_Non_Negative_Value()
    {
        // Act
        var result = _provider.GetAspNetCoreConnectionCountLegacy();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetAspNetCoreConnectionCountLegacy_Should_Return_Value_Within_Reasonable_Range()
    {
        // Act
        var result = _provider.GetAspNetCoreConnectionCountLegacy();

        // Assert
        // Should be within the configured maximum limit
        Assert.True(result <= _options.MaxEstimatedHttpConnections);
    }

    [Fact]
    public void GetAspNetCoreConnectionCountLegacy_Should_Handle_Exceptions_Gracefully()
    {
        // This test verifies that the method doesn't throw exceptions
        // and returns a valid fallback value when internal operations fail

        // Act & Assert - Should not throw
        var result = _provider.GetAspNetCoreConnectionCountLegacy();
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetAspNetCoreConnectionCountLegacy_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task<int>[5];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                return _provider.GetAspNetCoreConnectionCountLegacy();
            });
        }

        // Act
        System.Threading.Tasks.Task.WaitAll(tasks);

        // Assert
        foreach (var task in tasks)
        {
            Assert.True(task.Result >= 0);
        }
    }

    #endregion
}

