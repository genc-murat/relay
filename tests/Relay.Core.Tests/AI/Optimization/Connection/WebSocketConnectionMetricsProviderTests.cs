using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Connection;
using System;
using System.Collections.Concurrent;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;


public class WebSocketConnectionMetricsProviderTests
{
    private readonly ILogger _logger;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly WebSocketConnectionMetricsProvider _provider;

    public WebSocketConnectionMetricsProviderTests()
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

        _provider = new WebSocketConnectionMetricsProvider(
            _logger,
            _options,
            _requestAnalytics,
            _timeSeriesDb,
            _systemMetrics);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WebSocketConnectionMetricsProvider(
                null!,
                _options,
                _requestAnalytics,
                _timeSeriesDb,
                _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WebSocketConnectionMetricsProvider(
                _logger,
                null!,
                _requestAnalytics,
                _timeSeriesDb,
                _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WebSocketConnectionMetricsProvider(
                _logger,
                _options,
                null!,
                _timeSeriesDb,
                _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WebSocketConnectionMetricsProvider(
                _logger,
                _options,
                _requestAnalytics,
                null!,
                _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WebSocketConnectionMetricsProvider(
                _logger,
                _options,
                _requestAnalytics,
                _timeSeriesDb,
                null!));
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
        Assert.True(result <= _options.MaxEstimatedWebSocketConnections);
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
                // Test the public method concurrently
                var wsCount = _provider.GetWebSocketConnectionCount();

                Assert.True(wsCount >= 0);
            });
        }

        // Act & Assert
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    #endregion
}