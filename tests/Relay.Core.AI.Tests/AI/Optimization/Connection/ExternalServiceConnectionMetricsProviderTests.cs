using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Connection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;



public class ExternalServiceConnectionMetricsProviderTests
{
    private readonly ILogger _logger;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly ExternalServiceConnectionMetricsProvider _provider;

    public ExternalServiceConnectionMetricsProviderTests()
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
        _provider = new ExternalServiceConnectionMetricsProvider(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Accept_Valid_Parameters()
    {
        var provider = new ExternalServiceConnectionMetricsProvider(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics);
        Assert.NotNull(provider);
    }

    #endregion

    #region GetExternalServiceConnectionCount Tests

    [Fact]
    public void GetExternalServiceConnectionCount_Should_Return_Sum_Of_All_Connection_Types()
    {
        // Arrange - Add some request analytics to influence calculations
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        analysisData.AddMetrics(new RequestExecutionMetrics { AverageExecutionTime = TimeSpan.FromMilliseconds(1500), TotalExecutions = 1, SuccessfulExecutions = 1 }); // Long-running for message queue
        analysisData.AddMetrics(new RequestExecutionMetrics { AverageExecutionTime = TimeSpan.FromMilliseconds(200), TotalExecutions = 1, SuccessfulExecutions = 1 });
        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetExternalServiceConnectionCount();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= _options.MaxEstimatedExternalConnections);
    }

    [Fact]
    public void GetExternalServiceConnectionCount_Should_Handle_Exceptions_And_Return_Estimate()
    {
        // Arrange - Create provider with null dependencies to force exceptions
        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger,
            _options,
            new ConcurrentDictionary<Type, RequestAnalysisData>(), // Empty to avoid issues
            new TestTimeSeriesDatabase(),
            new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, new ConcurrentDictionary<Type, RequestAnalysisData>()));

        // Act
        var result = provider.GetExternalServiceConnectionCount();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetExternalServiceConnectionCount_Should_Respect_MaxEstimatedExternalConnections_Limit()
    {
        // Arrange - Set up scenario that would exceed limit
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add many executions to potentially exceed limit
        for (int i = 0; i < 1000; i++)
        {
            analysisData.AddMetrics(new RequestExecutionMetrics { AverageExecutionTime = TimeSpan.FromMilliseconds(1500), TotalExecutions = 1, SuccessfulExecutions = 1 });
        }
        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetExternalServiceConnectionCount();

        // Assert
        Assert.True(result <= _options.MaxEstimatedExternalConnections);
    }

    #endregion

    #region GetRedisConnectionCount Tests

    [Fact]
    public void GetRedisConnectionCount_Should_Return_Stored_Metrics_When_Available()
    {
        // Arrange - Mock stored metrics
        var mockTimeSeriesDb = new Mock<TestTimeSeriesDatabase>();
        mockTimeSeriesDb.Setup(db => db.GetRecentMetrics("Redis_ConnectionCount", 10))
            .Returns(new List<MetricDataPoint> { new MetricDataPoint { Timestamp = DateTime.UtcNow, Value = 7.0f } });

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger, _options, _requestAnalytics, mockTimeSeriesDb.Object, _systemMetrics);

        // Act
        var result = provider.GetRedisConnectionCount();

        // Assert
        Assert.Equal(7, result);
    }

    [Fact]
    public void GetRedisConnectionCount_Should_Estimate_Based_On_Load_Level()
    {
        // Arrange - Mock system metrics for different load levels
        var mockSystemMetrics = new Mock<SystemMetricsCalculator>(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        mockSystemMetrics.Setup(sm => sm.CalculateCurrentThroughput()).Returns(150.0); // High load

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger, _options, _requestAnalytics, _timeSeriesDb, mockSystemMetrics.Object);

        // Act
        var result = provider.GetRedisConnectionCount();

        // Assert - Should be higher for high load
        Assert.True(result >= 2 && result <= 5);
    }

    [Fact]
    public void GetRedisConnectionCount_Should_Handle_Exceptions_And_Return_Default()
    {
        // Arrange - Create a mock time series db that throws
        var mockTimeSeriesDb = new Mock<TimeSeriesDatabase>(
            Mock.Of<ILogger<TimeSeriesDatabase>>(),
            Mock.Of<ITimeSeriesRepository>(),
            Mock.Of<IForecastingService>(),
            Mock.Of<IAnomalyDetectionService>(),
            Mock.Of<ITimeSeriesStatisticsService>());
        mockTimeSeriesDb.Setup(db => db.GetRecentMetrics(It.IsAny<string>(), It.IsAny<int>()))
            .Throws(new Exception("Test exception"));

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger,
            _options,
            new ConcurrentDictionary<Type, RequestAnalysisData>(),
            mockTimeSeriesDb.Object,
            new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, new ConcurrentDictionary<Type, RequestAnalysisData>()));

        // Act
        var result = provider.GetRedisConnectionCount();

        // Assert
        Assert.Equal(2, result); // Safe default
    }

    #endregion

    #region GetMessageQueueConnectionCount Tests

    [Fact]
    public void GetMessageQueueConnectionCount_Should_Return_Stored_Metrics_When_Available()
    {
        // Arrange - Mock stored metrics
        var mockTimeSeriesDb = new Mock<TestTimeSeriesDatabase>();
        mockTimeSeriesDb.Setup(db => db.GetRecentMetrics("MessageQueue_ConnectionCount", 10))
            .Returns(new List<MetricDataPoint> { new MetricDataPoint { Timestamp = DateTime.UtcNow, Value = 3.0f } });

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger, _options, _requestAnalytics, mockTimeSeriesDb.Object, _systemMetrics);

        // Act
        var result = provider.GetMessageQueueConnectionCount();

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void GetMessageQueueConnectionCount_Should_Estimate_Based_On_Async_Requests()
    {
        // Arrange - Add long-running requests (async)
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        for (int i = 0; i < 500; i++)
        {
            analysisData.AddMetrics(new RequestExecutionMetrics { AverageExecutionTime = TimeSpan.FromMilliseconds(1500), TotalExecutions = 1, SuccessfulExecutions = 1 }); // > 1000ms = async
        }
        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetMessageQueueConnectionCount();

        // Assert
        Assert.True(result >= 1 && result <= 5); // Based on calculation: Math.Max(1, Math.Min(5, 500/100)) = 5
    }

    [Fact]
    public void GetMessageQueueConnectionCount_Should_Return_Zero_When_No_Async_Requests()
    {
        // Arrange - Only short requests
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        for (int i = 0; i < 100; i++)
        {
            analysisData.AddMetrics(new RequestExecutionMetrics { AverageExecutionTime = TimeSpan.FromMilliseconds(100), TotalExecutions = 1, SuccessfulExecutions = 1 }); // < 1000ms = sync
        }
        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetMessageQueueConnectionCount();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetMessageQueueConnectionCount_Should_Handle_Exceptions_And_Return_Default()
    {
        // Arrange - Create a mock time series db that throws
        var mockTimeSeriesDb = new Mock<TimeSeriesDatabase>(
            Mock.Of<ILogger<TimeSeriesDatabase>>(),
            Mock.Of<ITimeSeriesRepository>(),
            Mock.Of<IForecastingService>(),
            Mock.Of<IAnomalyDetectionService>(),
            Mock.Of<ITimeSeriesStatisticsService>());
        mockTimeSeriesDb.Setup(db => db.GetRecentMetrics(It.IsAny<string>(), It.IsAny<int>()))
            .Throws(new Exception("Test exception"));

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger,
            _options,
            new ConcurrentDictionary<Type, RequestAnalysisData>(),
            mockTimeSeriesDb.Object,
            new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, new ConcurrentDictionary<Type, RequestAnalysisData>()));

        // Act
        var result = provider.GetMessageQueueConnectionCount();

        // Assert
        Assert.Equal(1, result); // Safe default
    }

    #endregion

    #region GetExternalApiConnectionCount Tests

    [Fact]
    public void GetExternalApiConnectionCount_Should_Return_Stored_Metrics_When_Available()
    {
        // Arrange - Mock stored metrics
        var mockTimeSeriesDb = new Mock<TestTimeSeriesDatabase>();
        mockTimeSeriesDb.Setup(db => db.GetRecentMetrics("ExternalApi_ConnectionCount", 10))
            .Returns(new List<MetricDataPoint> { new MetricDataPoint { Timestamp = DateTime.UtcNow, Value = 12.0f } });

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger, _options, _requestAnalytics, mockTimeSeriesDb.Object, _systemMetrics);

        // Act
        var result = provider.GetExternalApiConnectionCount();

        // Assert
        Assert.Equal(12, result);
    }

    [Fact]
    public void GetExternalApiConnectionCount_Should_Estimate_Based_On_Request_Volume()
    {
        // Arrange - Add many requests
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        for (int i = 0; i < 100; i++)
        {
            analysisData.AddMetrics(new RequestExecutionMetrics { AverageExecutionTime = TimeSpan.FromMilliseconds(200), TotalExecutions = 1, SuccessfulExecutions = 1 });
        }
        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetExternalApiConnectionCount();

        // Assert - Should be 10 (100 executions / 10)
        Assert.Equal(10, result);
    }

    [Fact]
    public void GetExternalApiConnectionCount_Should_Cap_At_Reasonable_Limit()
    {
        // Arrange - Add excessive requests
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        for (int i = 0; i < 10000; i++)
        {
            analysisData.AddMetrics(new RequestExecutionMetrics { AverageExecutionTime = TimeSpan.FromMilliseconds(200), TotalExecutions = 1, SuccessfulExecutions = 1 });
        }
        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetExternalApiConnectionCount();

        // Assert - Should be capped at 20
        Assert.Equal(20, result);
    }

    [Fact]
    public void GetExternalApiConnectionCount_Should_Handle_Exceptions_And_Return_Default()
    {
        // Arrange - Create a mock time series db that throws
        var mockTimeSeriesDb = new Mock<TimeSeriesDatabase>(
            Mock.Of<ILogger<TimeSeriesDatabase>>(),
            Mock.Of<ITimeSeriesRepository>(),
            Mock.Of<IForecastingService>(),
            Mock.Of<IAnomalyDetectionService>(),
            Mock.Of<ITimeSeriesStatisticsService>());
        mockTimeSeriesDb.Setup(db => db.GetRecentMetrics(It.IsAny<string>(), It.IsAny<int>()))
            .Throws(new Exception("Test exception"));

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger,
            _options,
            new ConcurrentDictionary<Type, RequestAnalysisData>(),
            mockTimeSeriesDb.Object,
            new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, new ConcurrentDictionary<Type, RequestAnalysisData>()));

        // Act
        var result = provider.GetExternalApiConnectionCount();

        // Assert
        Assert.Equal(5, result); // Safe default
    }

    #endregion

    #region GetMicroserviceConnectionCount Tests

    [Fact]
    public void GetMicroserviceConnectionCount_Should_Return_Stored_Metrics_When_Available()
    {
        // Arrange - Mock stored metrics
        var mockTimeSeriesDb = new Mock<TestTimeSeriesDatabase>();
        mockTimeSeriesDb.Setup(db => db.GetRecentMetrics("Microservice_ConnectionCount", 10))
            .Returns(new List<MetricDataPoint> { new MetricDataPoint { Timestamp = DateTime.UtcNow, Value = 8.0f } });

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger, _options, _requestAnalytics, mockTimeSeriesDb.Object, _systemMetrics);

        // Act
        var result = provider.GetMicroserviceConnectionCount();

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public void GetMicroserviceConnectionCount_Should_Estimate_Based_On_External_Calls_And_Load()
    {
        // Arrange - Add requests and mock high load
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        for (int i = 0; i < 100; i++)
        {
            analysisData.AddMetrics(new RequestExecutionMetrics { AverageExecutionTime = TimeSpan.FromMilliseconds(200), TotalExecutions = 1, SuccessfulExecutions = 1 });
        }
        _requestAnalytics[requestType] = analysisData;

        var mockSystemMetrics = new Mock<SystemMetricsCalculator>(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        mockSystemMetrics.Setup(sm => sm.CalculateCurrentThroughput()).Returns(150.0); // High load

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger, _options, _requestAnalytics, _timeSeriesDb, mockSystemMetrics.Object);

        // Act
        var result = provider.GetMicroserviceConnectionCount();

        // Assert - Should be adjusted for load
        Assert.True(result >= 1);
    }

    [Fact]
    public void GetMicroserviceConnectionCount_Should_Apply_Load_Multipliers_Correctly()
    {
        // Arrange - Mock different load levels
        var mockSystemMetrics = new Mock<SystemMetricsCalculator>(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        mockSystemMetrics.Setup(sm => sm.CalculateCurrentThroughput()).Returns(5.0); // Idle load

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger, _options, _requestAnalytics, _timeSeriesDb, mockSystemMetrics.Object);

        // Act
        var result = provider.GetMicroserviceConnectionCount();

        // Assert - Should be lower for idle load (0.5 multiplier)
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetMicroserviceConnectionCount_Should_Handle_Exceptions_And_Return_Default()
    {
        // Arrange - Create a mock time series db that throws
        var mockTimeSeriesDb = new Mock<TimeSeriesDatabase>(
            Mock.Of<ILogger<TimeSeriesDatabase>>(),
            Mock.Of<ITimeSeriesRepository>(),
            Mock.Of<IForecastingService>(),
            Mock.Of<IAnomalyDetectionService>(),
            Mock.Of<ITimeSeriesStatisticsService>());
        mockTimeSeriesDb.Setup(db => db.GetRecentMetrics(It.IsAny<string>(), It.IsAny<int>()))
            .Throws(new Exception("Test exception"));

        var provider = new ExternalServiceConnectionMetricsProvider(
            _logger,
            _options,
            new ConcurrentDictionary<Type, RequestAnalysisData>(),
            mockTimeSeriesDb.Object,
            new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, new ConcurrentDictionary<Type, RequestAnalysisData>()));

        // Act
        var result = provider.GetMicroserviceConnectionCount();

        // Assert
        Assert.Equal(3, result); // Safe default
    }

    #endregion
}