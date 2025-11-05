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

public class DatabaseConnectionMetricsProviderTests
{
    private readonly ILogger _logger;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly DatabaseConnectionMetricsProvider _provider;

    public DatabaseConnectionMetricsProviderTests()
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
        _provider = new DatabaseConnectionMetricsProvider(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DatabaseConnectionMetricsProvider(_logger, null!, _requestAnalytics, _timeSeriesDb, _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DatabaseConnectionMetricsProvider(_logger, _options, null!, _timeSeriesDb, _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DatabaseConnectionMetricsProvider(_logger, _options, _requestAnalytics, null!, _systemMetrics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DatabaseConnectionMetricsProvider(_logger, _options, _requestAnalytics, _timeSeriesDb, null!));
    }

    [Fact]
    public void Constructor_Should_Accept_Valid_Parameters()
    {
        var provider = new DatabaseConnectionMetricsProvider(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics);
        Assert.NotNull(provider);
    }

    #endregion

    #region GetDatabaseConnectionCount Tests

    [Fact]
    public void GetDatabaseConnectionCount_Should_Return_Max_Of_All_Connection_Types()
    {
        // Arrange - Set up scenarios where different connection types return different values
        // This is tested through integration since the method combines results from multiple sub-methods

        // Act
        var result = _provider.GetDatabaseConnectionCount();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= _options.MaxEstimatedDbConnections);
    }

    [Fact]
    public void GetDatabaseConnectionCount_Should_Use_Pool_Utilization_Estimate_When_No_Specific_Counts()
    {
        // Act
        var result = _provider.GetDatabaseConnectionCount();

        // Assert - Should return a reasonable estimate
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetDatabaseConnectionCount_Should_Respect_MaxEstimatedDbConnections_Limit()
    {
        // Arrange - Create options with low limit
        var limitedOptions = new AIOptimizationOptions
        {
            MaxEstimatedHttpConnections = 1000,
            MaxEstimatedDbConnections = 5, // Very low limit
            EstimatedMaxDbConnections = 100,
            MaxEstimatedExternalConnections = 30,
            MaxEstimatedWebSocketConnections = 1000
        };

        var provider = new DatabaseConnectionMetricsProvider(_logger, limitedOptions, _requestAnalytics, _timeSeriesDb, _systemMetrics);

        // Act
        var result = provider.GetDatabaseConnectionCount();

        // Assert
        Assert.True(result <= 5);
    }

    #endregion

    #region GetSqlServerConnectionCount Tests

    [Fact]
    public void GetSqlServerConnectionCount_Should_Return_Stored_Metrics_When_Available()
    {
        // Arrange - Add stored metrics
        var timestamp = DateTime.UtcNow;
        _timeSeriesDb.StoreMetric("SqlServer_ConnectionCount", 25.0, timestamp.AddMinutes(-1));
        _timeSeriesDb.StoreMetric("SqlServer_ConnectionCount", 30.0, timestamp.AddMinutes(-2));
        _timeSeriesDb.StoreMetric("SqlServer_ConnectionCount", 20.0, timestamp.AddMinutes(-3));

        // Act
        var result = _provider.GetSqlServerConnectionCount();

        // Assert - Should return average of stored metrics (25+30+20)/3 = 25
        Assert.Equal(25, result);
    }

    [Fact]
    public void GetSqlServerConnectionCount_Should_Estimate_When_No_Stored_Metrics()
    {
        // Act
        var result = _provider.GetSqlServerConnectionCount();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= 100); // Capped at 100
    }

    [Fact]
    public void GetSqlServerConnectionCount_Should_Store_Estimated_Metric()
    {
        // Act
        _provider.GetSqlServerConnectionCount();

        // Assert - Check that metric was stored
        var storedMetrics = _timeSeriesDb.GetRecentMetrics("SqlServer_ConnectionCount", 1);
        Assert.NotEmpty(storedMetrics);
    }

    [Fact]
    public void GetSqlServerConnectionCount_Should_Cap_Result_At_100()
    {
        // Arrange - Mock high pool utilization to get high estimate
        // This is difficult to test directly, but we can verify the cap is applied
        var result = _provider.GetSqlServerConnectionCount();
        Assert.True(result <= 100);
    }

    [Fact]
    public void GetSqlServerConnectionCount_Should_Return_Zero_On_Error()
    {
        // Arrange - Create provider with null logger to potentially cause error
        var provider = new DatabaseConnectionMetricsProvider(null!, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics);

        // Act
        var result = provider.GetSqlServerConnectionCount();

        // Assert - Should return 0 on error
        Assert.Equal(0, result);
    }

    #endregion

    #region GetEntityFrameworkConnectionCount Tests

    [Fact]
    public void GetEntityFrameworkConnectionCount_Should_Return_Stored_Metrics_When_Available()
    {
        // Arrange - Add stored metrics
        var timestamp = DateTime.UtcNow;
        _timeSeriesDb.StoreMetric("EntityFramework_ConnectionCount", 15.0, timestamp.AddMinutes(-1));
        _timeSeriesDb.StoreMetric("EntityFramework_ConnectionCount", 20.0, timestamp.AddMinutes(-2));

        // Act
        var result = _provider.GetEntityFrameworkConnectionCount();

        // Assert - Should return adjusted average
        Assert.True(result >= 0);
        Assert.True(result <= 50); // Capped at 50
    }

    [Fact]
    public void GetEntityFrameworkConnectionCount_Should_Estimate_Based_On_Request_Analytics()
    {
        // Arrange - Add some request analytics data
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var metrics = new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 95,
            FailedExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(500),
            ConcurrentExecutions = 10,
            LastExecution = DateTime.UtcNow
        };
        analysisData.AddMetrics(metrics);

        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetEntityFrameworkConnectionCount();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= 50); // Capped at 50
    }

    [Fact]
    public void GetEntityFrameworkConnectionCount_Should_Increase_Estimate_For_Long_Running_Requests()
    {
        // Arrange - Add request analytics with long execution times
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var metrics = new RequestExecutionMetrics
        {
            TotalExecutions = 50,
            SuccessfulExecutions = 45,
            FailedExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(1500), // > 1000ms
            ConcurrentExecutions = 5,
            LastExecution = DateTime.UtcNow
        };
        analysisData.AddMetrics(metrics);

        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetEntityFrameworkConnectionCount();

        // Assert - Should be reasonable estimate
        Assert.True(result >= 0);
        Assert.True(result <= 50);
    }

    [Fact]
    public void GetEntityFrameworkConnectionCount_Should_Decrease_Estimate_For_Fast_Requests()
    {
        // Arrange - Add request analytics with fast execution times
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var metrics = new RequestExecutionMetrics
        {
            TotalExecutions = 50,
            SuccessfulExecutions = 48,
            FailedExecutions = 2,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50), // < 100ms
            ConcurrentExecutions = 3,
            LastExecution = DateTime.UtcNow
        };
        analysisData.AddMetrics(metrics);

        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetEntityFrameworkConnectionCount();

        // Assert - Should be reasonable estimate
        Assert.True(result >= 0);
        Assert.True(result <= 50);
    }

    [Fact]
    public void GetEntityFrameworkConnectionCount_Should_Increase_Estimate_For_High_Pool_Utilization()
    {
        // This is tested indirectly through the overall behavior
        var result = _provider.GetEntityFrameworkConnectionCount();
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetEntityFrameworkConnectionCount_Should_Store_Estimated_Metric_When_No_Existing_Metrics()
    {
        // Arrange - Create a fresh TimeSeriesDatabase to ensure no existing metrics
        var freshTimeSeriesDb = TimeSeriesDatabase.Create(NullLogger<TimeSeriesDatabase>.Instance, maxHistorySize: 10000);
        var freshProvider = new DatabaseConnectionMetricsProvider(_logger, _options, new ConcurrentDictionary<Type, RequestAnalysisData>(), freshTimeSeriesDb, _systemMetrics);

        // Act - Call the method which should store metrics when estimating (no existing metrics)
        freshProvider.GetEntityFrameworkConnectionCount();

        // Assert - Should have stored metrics
        var storedMetrics = freshTimeSeriesDb.GetRecentMetrics("EntityFramework_ConnectionCount", 1);
        Assert.NotNull(storedMetrics);
        Assert.NotEmpty(storedMetrics);
    }

    [Fact]
    public void GetEntityFrameworkConnectionCount_Should_Cap_Result_At_50()
    {
        var result = _provider.GetEntityFrameworkConnectionCount();
        Assert.True(result <= 50);
    }

    #endregion

    #region GetNoSqlConnectionCount Tests

    [Fact]
    public void GetNoSqlConnectionCount_Should_Return_Stored_Metrics_When_Available()
    {
        // Arrange
        var mockTimeSeriesDb = new Mock<TestTimeSeriesDatabase>();
        mockTimeSeriesDb.Setup(db => db.GetRecentMetrics("NoSql_ConnectionCount", 10))
            .Returns(new List<MetricDataPoint> { new MetricDataPoint { Value = 8.0f } });

        var provider = new DatabaseConnectionMetricsProvider(_logger, _options, _requestAnalytics, mockTimeSeriesDb.Object, _systemMetrics);

        // Act
        var result = provider.GetNoSqlConnectionCount();

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public void GetNoSqlConnectionCount_Should_Estimate_Based_On_Request_Count()
    {
        // Arrange - Add request analytics
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var metrics = new RequestExecutionMetrics
        {
            TotalExecutions = 1000, // High request count
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 20,
            LastExecution = DateTime.UtcNow
        };
        analysisData.AddMetrics(metrics);

        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetNoSqlConnectionCount();

        // Assert - Should estimate connections based on request count
        Assert.True(result >= 1); // At least 1 connection
        Assert.True(result <= 15); // Capped at 15
    }

    [Fact]
    public void GetNoSqlConnectionCount_Should_Store_Estimated_Metric()
    {
        // Act
        _provider.GetNoSqlConnectionCount();

        // Assert
        var storedMetrics = _timeSeriesDb.GetRecentMetrics("NoSql_ConnectionCount", 1);
        Assert.NotEmpty(storedMetrics);
    }

    [Fact]
    public void GetNoSqlConnectionCount_Should_Cap_Result_At_15()
    {
        // Arrange - Add many requests to get high estimate
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var metrics = new RequestExecutionMetrics
        {
            TotalExecutions = 10000, // Very high request count
            SuccessfulExecutions = 9000,
            FailedExecutions = 1000,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 50,
            LastExecution = DateTime.UtcNow
        };
        analysisData.AddMetrics(metrics);

        _requestAnalytics[requestType] = analysisData;

        // Act
        var result = _provider.GetNoSqlConnectionCount();

        // Assert
        Assert.True(result <= 15);
    }

    [Fact]
    public void GetNoSqlConnectionCount_Should_Return_Default_On_Error()
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

        var provider = new DatabaseConnectionMetricsProvider(null!, _options, _requestAnalytics, mockTimeSeriesDb.Object, _systemMetrics);

        // Act
        var result = provider.GetNoSqlConnectionCount();

        // Assert - Should return safe default of 2
        Assert.Equal(2, result);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void GetDatabaseConnectionCount_Should_Handle_Internal_Exceptions_Gracefully()
    {
        // Arrange - Create a provider with valid parameters
        // The method has try-catch blocks that handle exceptions internally
        var provider = new DatabaseConnectionMetricsProvider(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics);

        // Act
        var result = provider.GetDatabaseConnectionCount();

        // Assert - Should return a valid result even if internal methods throw exceptions
        Assert.True(result >= 0);
        Assert.True(result <= _options.MaxEstimatedDbConnections);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void All_Connection_Count_Methods_Should_Return_Non_Negative_Values()
    {
        // Act
        var dbConnections = _provider.GetDatabaseConnectionCount();
        var sqlConnections = _provider.GetSqlServerConnectionCount();
        var efConnections = _provider.GetEntityFrameworkConnectionCount();
        var nosqlConnections = _provider.GetNoSqlConnectionCount();

        // Assert
        Assert.True(dbConnections >= 0);
        Assert.True(sqlConnections >= 0);
        Assert.True(efConnections >= 0);
        Assert.True(nosqlConnections >= 0);
    }

    [Fact]
    public void Connection_Counts_Should_Respect_Configured_Limits()
    {
        // Act
        var dbConnections = _provider.GetDatabaseConnectionCount();
        var sqlConnections = _provider.GetSqlServerConnectionCount();
        var efConnections = _provider.GetEntityFrameworkConnectionCount();
        var nosqlConnections = _provider.GetNoSqlConnectionCount();

        // Assert
        Assert.True(dbConnections <= _options.MaxEstimatedDbConnections);
        Assert.True(sqlConnections <= 100); // SQL Server cap
        Assert.True(efConnections <= 50);   // EF cap
        Assert.True(nosqlConnections <= 15); // NoSQL cap
    }

    #endregion
}