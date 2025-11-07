using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Models;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI;

public class TimeSeriesDatabaseBasicOperationsTests : IDisposable
{
    private readonly ILogger<TimeSeriesDatabase> _logger;
    private readonly TimeSeriesDatabase _database;

    public TimeSeriesDatabaseBasicOperationsTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
        _database = TimeSeriesDatabase.Create(_logger, maxHistorySize: 1000);
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Create_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TimeSeriesDatabase.Create(null!));
    }

    [Fact]
    public void Create_Should_Initialize_With_Default_MaxHistorySize()
    {
        // Arrange & Act
        using var db = TimeSeriesDatabase.Create(_logger);

        // Assert - Should not throw
        db.StoreMetric("test", 1.0, DateTime.UtcNow);
    }

    [Fact]
    public void Create_Should_Initialize_With_Custom_MaxHistorySize()
    {
        // Arrange & Act
        using var db = TimeSeriesDatabase.Create(_logger, maxHistorySize: 500);

        // Assert - Should not throw
        db.StoreMetric("test", 1.0, DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Arrange
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimeSeriesDatabase(
            null!, repository, forecastingService, anomalyDetectionService, statisticsService));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Repository_Is_Null()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimeSeriesDatabase(
            logger, null!, forecastingService, anomalyDetectionService, statisticsService));
    }

    [Fact]
    public void Constructor_Should_Throw_When_ForecastingService_Is_Null()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimeSeriesDatabase(
            logger, repository, null!, anomalyDetectionService, statisticsService));
    }

    [Fact]
    public void Constructor_Should_Throw_When_AnomalyDetectionService_Is_Null()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimeSeriesDatabase(
            logger, repository, forecastingService, null!, statisticsService));
    }

    [Fact]
    public void Constructor_Should_Throw_When_StatisticsService_Is_Null()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimeSeriesDatabase(
            logger, repository, forecastingService, anomalyDetectionService, null!));
    }

    [Fact]
    public void Create_Should_Throw_When_MaxHistorySize_Is_Zero()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSeriesDatabase.Create(_logger, maxHistorySize: 0));
    }

    [Fact]
    public void Create_Should_Throw_When_MaxHistorySize_Is_Negative()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSeriesDatabase.Create(_logger, maxHistorySize: -1));
    }

    [Fact]
    public void Create_Should_Throw_When_MaxHistorySize_Exceeds_Maximum()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSeriesDatabase.Create(_logger, maxHistorySize: 1000001));
    }

    [Fact]
    public void Create_Should_Use_Config_Value_For_MaxHistorySize()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeSeries:MaxHistorySize"] = "500"
            })
            .Build();

        // Act
        using var db = TimeSeriesDatabase.Create(_logger, configuration: config);

        // Assert - Should not throw and use config value
        db.StoreMetric("test", 1.0, DateTime.UtcNow);
    }

    [Fact]
    public void Create_Should_Throw_When_Config_MaxHistorySize_Is_Invalid()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeSeries:MaxHistorySize"] = "0"
            })
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSeriesDatabase.Create(_logger, configuration: config));
    }

    [Fact]
    public void Create_Should_Throw_When_Config_ForecastHorizon_Is_Zero()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeSeries:ForecastHorizon"] = "0"
            })
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSeriesDatabase.Create(_logger, configuration: config));
    }

    [Fact]
    public void Create_Should_Throw_When_Config_ForecastHorizon_Is_Negative()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeSeries:ForecastHorizon"] = "-1"
            })
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSeriesDatabase.Create(_logger, configuration: config));
    }

    [Fact]
    public void Create_Should_Throw_When_Config_ForecastHorizon_Exceeds_Maximum()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeSeries:ForecastHorizon"] = "1001"
            })
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSeriesDatabase.Create(_logger, configuration: config));
    }

    [Fact]
    public void Create_Should_Throw_When_Config_ForecastingMethod_Is_Invalid()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeSeries:ForecastingMethod"] = "InvalidMethod"
            })
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSeriesDatabase.Create(_logger, configuration: config));
    }

    [Fact]
    public void Create_Should_Throw_When_Config_Has_Invalid_Data_Types()
    {
        // Arrange - Configuration with invalid data types
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeSeries:MaxHistorySize"] = "notanumber"
            })
            .Build();

        // Act & Assert - Should throw InvalidOperationException for invalid conversion
        Assert.Throws<InvalidOperationException>(() => TimeSeriesDatabase.Create(_logger, configuration: config));
    }

    [Fact]
    public void Create_Should_Use_Config_Values_For_Forecasting()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeSeries:ForecastHorizon"] = "48",
                ["TimeSeries:ForecastingMethod"] = "SSA"
            })
            .Build();

        // Act
        using var db = TimeSeriesDatabase.Create(_logger, configuration: config);

        // Assert - Should not throw and use config values
        db.StoreMetric("test", 1.0, DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_Should_Log_Initialization_Message()
    {
        // Arrange
        var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        // Act
        var db = new TimeSeriesDatabase(loggerMock.Object, repository, forecastingService, anomalyDetectionService, statisticsService);

        // Assert - Should have logged the initialization message
        loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Time-series database initialized")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    #endregion

    #region StoreMetric Tests

    [Fact]
    public void StoreMetric_Should_Store_Basic_Metric()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        _database.StoreMetric("cpu.usage", 0.75, timestamp);

        // Assert
        var history = _database.GetHistory("cpu.usage").ToList();
        Assert.Single(history);
        Assert.Equal("cpu.usage", history[0].MetricName);
        Assert.Equal(0.75f, history[0].Value, 2);
        Assert.Equal(timestamp, history[0].Timestamp);
    }

    [Fact]
    public void StoreMetric_Should_Store_With_Moving_Averages()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        _database.StoreMetric("memory.usage", 0.60, timestamp,
            movingAverage5: 0.58, movingAverage15: 0.55);

        // Assert
        var history = _database.GetHistory("memory.usage").ToList();
        Assert.Single(history);
        Assert.Equal(0.60f, history[0].Value, 2);
        Assert.Equal(0.58f, history[0].MA5, 2);
        Assert.Equal(0.55f, history[0].MA15, 2);
    }

    [Fact]
    public void StoreMetric_Should_Store_With_Trend()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        _database.StoreMetric("requests.rate", 100.0, timestamp,
            trend: TrendDirection.Increasing);

        // Assert
        var history = _database.GetHistory("requests.rate").ToList();
        Assert.Single(history);
        Assert.Equal((int)TrendDirection.Increasing, history[0].Trend);
    }

    [Fact]
    public void StoreMetric_Should_Set_HourOfDay_And_DayOfWeek()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc); // Monday

        // Act
        _database.StoreMetric("test.metric", 50.0, timestamp);

        // Assert
        var history = _database.GetHistory("test.metric").ToList();
        Assert.Single(history);
        Assert.Equal(14, history[0].HourOfDay);
        Assert.Equal((int)DayOfWeek.Monday, history[0].DayOfWeek);
    }

    [Fact]
    public void StoreMetric_Should_Use_Value_As_Default_For_MovingAverages()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        _database.StoreMetric("test.metric", 75.0, timestamp);

        // Assert
        var history = _database.GetHistory("test.metric").ToList();
        Assert.Single(history);
        Assert.Equal(75.0f, history[0].Value, 2);
        Assert.Equal(75.0f, history[0].MA5, 2);
        Assert.Equal(75.0f, history[0].MA15, 2);
    }

    [Fact]
    public void StoreMetric_Should_Handle_Multiple_Metrics()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        _database.StoreMetric("metric1", 10.0, timestamp);
        _database.StoreMetric("metric2", 20.0, timestamp);
        _database.StoreMetric("metric3", 30.0, timestamp);

        // Assert
        var history1 = _database.GetHistory("metric1").ToList();
        var history2 = _database.GetHistory("metric2").ToList();
        var history3 = _database.GetHistory("metric3").ToList();

        Assert.Single(history1);
        Assert.Single(history2);
        Assert.Single(history3);
        Assert.Equal(10.0f, history1[0].Value, 2);
        Assert.Equal(20.0f, history2[0].Value, 2);
        Assert.Equal(30.0f, history3[0].Value, 2);
    }

    [Fact]
    public void StoreMetric_Should_Handle_Multiple_Values_For_Same_Metric()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;

        // Act
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));
        _database.StoreMetric("test", 30.0, baseTime.AddMinutes(2));

        // Assert
        var history = _database.GetHistory("test").ToList();
        Assert.Equal(3, history.Count);
        Assert.Equal(10.0f, history[0].Value, 2);
        Assert.Equal(20.0f, history[1].Value, 2);
        Assert.Equal(30.0f, history[2].Value, 2);
    }

    [Fact]
    public void StoreMetric_Should_Throw_For_Empty_Metric_Name()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _database.StoreMetric("", 42.0, timestamp));
    }

    [Fact]
    public void StoreMetric_Should_Handle_Extreme_Values()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        _database.StoreMetric("extreme", double.MaxValue, timestamp);
        _database.StoreMetric("extreme", double.MinValue, timestamp.AddMinutes(1));
        _database.StoreMetric("extreme", double.NaN, timestamp.AddMinutes(2));
        _database.StoreMetric("extreme", double.PositiveInfinity, timestamp.AddMinutes(3));
        _database.StoreMetric("extreme", double.NegativeInfinity, timestamp.AddMinutes(4));

        // Assert
        var history = _database.GetHistory("extreme").ToList();
        Assert.Equal(5, history.Count);
        // Values should be stored (may be converted to float)
    }

    [Fact]
    public void StoreMetric_Should_Handle_Future_Timestamps()
    {
        // Arrange
        var futureTimestamp = DateTime.UtcNow.AddDays(1);

        // Act
        _database.StoreMetric("future", 100.0, futureTimestamp);

        // Assert
        var history = _database.GetHistory("future").ToList();
        Assert.Single(history);
        Assert.Equal(futureTimestamp, history[0].Timestamp);
    }

    #endregion

    #region StoreBatch Tests

    [Fact]
    public void StoreBatch_Should_Store_Multiple_Metrics_At_Once()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new Dictionary<string, double>
        {
            ["cpu.usage"] = 0.75,
            ["memory.usage"] = 0.60,
            ["disk.usage"] = 0.45
        };

        // Act
        _database.StoreBatch(metrics, timestamp);

        // Assert
        Assert.Single(_database.GetHistory("cpu.usage"));
        Assert.Single(_database.GetHistory("memory.usage"));
        Assert.Single(_database.GetHistory("disk.usage"));
    }

    [Fact]
    public void StoreBatch_Should_Store_With_Moving_Averages()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new Dictionary<string, double>
        {
            ["test.metric"] = 100.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["test.metric"] = new MovingAverageData { MA5 = 95.0, MA15 = 90.0 }
        };

        // Act
        _database.StoreBatch(metrics, timestamp, movingAverages);

        // Assert
        var history = _database.GetHistory("test.metric").ToList();
        Assert.Single(history);
        Assert.Equal(95.0f, history[0].MA5, 2);
        Assert.Equal(90.0f, history[0].MA15, 2);
    }

    [Fact]
    public void StoreBatch_Should_Store_With_Trend_Directions()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new Dictionary<string, double>
        {
            ["metric1"] = 100.0,
            ["metric2"] = 200.0
        };
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["metric1"] = TrendDirection.Increasing,
            ["metric2"] = TrendDirection.Decreasing
        };

        // Act
        _database.StoreBatch(metrics, timestamp, trendDirections: trendDirections);

        // Assert
        var history1 = _database.GetHistory("metric1").ToList();
        var history2 = _database.GetHistory("metric2").ToList();
        Assert.Equal((int)TrendDirection.Increasing, history1[0].Trend);
        Assert.Equal((int)TrendDirection.Decreasing, history2[0].Trend);
    }

    [Fact]
    public void StoreBatch_Should_Handle_Empty_Dictionary()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new Dictionary<string, double>();

        // Act
        _database.StoreBatch(metrics, timestamp);

        // Assert - Should not throw
    }

    [Fact]
    public void StoreBatch_Should_Accept_Null_Metrics_Dictionary()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act & Assert - Should not throw
        _database.StoreBatch(null!, timestamp);
    }

    [Fact]
    public void StoreBatch_Should_Throw_For_Empty_Metric_Names_In_Dictionary()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new Dictionary<string, double>
        {
            [""] = 42.0,
            ["valid.metric"] = 24.0
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _database.StoreBatch(metrics, timestamp));
    }

    [Fact]
    public void StoreBatch_Should_Handle_Extreme_Values_In_Dictionary()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new Dictionary<string, double>
        {
            ["max"] = double.MaxValue,
            ["min"] = double.MinValue,
            ["nan"] = double.NaN,
            ["inf"] = double.PositiveInfinity
        };

        // Act
        _database.StoreBatch(metrics, timestamp);

        // Assert - Should not throw and store all values
        Assert.Single(_database.GetHistory("max"));
        Assert.Single(_database.GetHistory("min"));
        Assert.Single(_database.GetHistory("nan"));
        Assert.Single(_database.GetHistory("inf"));
    }

    #endregion

    #region GetHistory Tests

    [Fact]
    public void GetHistory_Should_Return_Empty_For_Unknown_Metric()
    {
        // Act
        var history = _database.GetHistory("unknown.metric");

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public void GetHistory_Should_Return_All_Data_When_No_Lookback()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime.AddMinutes(-10));
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(-5));
        _database.StoreMetric("test", 30.0, baseTime);

        // Act
        var history = _database.GetHistory("test").ToList();

        // Assert
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public void GetHistory_Should_Filter_By_Lookback_Period()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime.AddMinutes(-10));
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(-5));
        _database.StoreMetric("test", 30.0, baseTime.AddMinutes(-1));

        // Act
        var history = _database.GetHistory("test", TimeSpan.FromMinutes(6)).ToList();

        // Assert
        Assert.Equal(2, history.Count); // Only last 2 within 6 minutes
        Assert.Equal(20.0f, history[0].Value, 2);
        Assert.Equal(30.0f, history[1].Value, 2);
    }

    [Fact]
    public void GetHistory_Should_Return_Ordered_By_Timestamp()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 30.0, baseTime.AddMinutes(2));
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));

        // Act
        var history = _database.GetHistory("test").ToList();

        // Assert
        Assert.Equal(3, history.Count);
        Assert.True(history[0].Timestamp <= history[1].Timestamp);
        Assert.True(history[1].Timestamp <= history[2].Timestamp);
    }

    [Fact]
    public void GetHistory_Should_Throw_For_Empty_Metric_Name()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _database.GetHistory(""));
    }

    [Fact]
    public void GetHistory_Should_Handle_Very_Long_Lookback_Period()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime.AddYears(-10));
        _database.StoreMetric("test", 20.0, baseTime);

        // Act
        var history = _database.GetHistory("test", TimeSpan.FromDays(365 * 20)).ToList();

        // Assert - Should return all data
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public void GetHistory_Should_Handle_Zero_Lookback_Period()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime.AddMinutes(-10));
        _database.StoreMetric("test", 20.0, baseTime);

        // Act
        var history = _database.GetHistory("test", TimeSpan.Zero).ToList();

        // Assert - Should return no data (zero lookback)
        Assert.Empty(history);
    }

    #endregion

    #region GetRecentMetrics Tests

    [Fact]
    public void GetRecentMetrics_Should_Return_Empty_For_Unknown_Metric()
    {
        // Act
        var recent = _database.GetRecentMetrics("unknown.metric", 10);

        // Assert
        Assert.Empty(recent);
    }

    [Fact]
    public void GetRecentMetrics_Should_Return_Most_Recent_N_Metrics()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        for (int i = 0; i < 10; i++)
        {
            _database.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
        }

        // Act
        var recent = _database.GetRecentMetrics("test", 5);

        // Assert
        Assert.Equal(5, recent.Count);
        Assert.Equal(50.0f, recent[0].Value, 2); // Values 50-90
        Assert.Equal(90.0f, recent[4].Value, 2);
    }

    [Fact]
    public void GetRecentMetrics_Should_Return_Ordered_Chronologically()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));
        _database.StoreMetric("test", 30.0, baseTime.AddMinutes(2));

        // Act
        var recent = _database.GetRecentMetrics("test", 3);

        // Assert
        Assert.Equal(3, recent.Count);
        Assert.True(recent[0].Timestamp <= recent[1].Timestamp);
        Assert.True(recent[1].Timestamp <= recent[2].Timestamp);
    }

    [Fact]
    public void GetRecentMetrics_Should_Return_All_If_Count_Exceeds_Available()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));

        // Act
        var recent = _database.GetRecentMetrics("test", 10);

        // Assert
        Assert.Equal(2, recent.Count);
    }

    [Fact]
    public void GetRecentMetrics_Should_Return_Empty_When_Count_Is_Zero()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));

        // Act
        var recent = _database.GetRecentMetrics("test", 0);

        // Assert
        Assert.Empty(recent);
    }

    [Fact]
    public void GetRecentMetrics_Should_Return_Empty_When_Count_Is_Negative()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));

        // Act
        var recent = _database.GetRecentMetrics("test", -5);

        // Assert
        Assert.Empty(recent);
    }

    [Fact]
    public void GetRecentMetrics_Should_Handle_Very_Large_Count()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));
        _database.StoreMetric("test", 30.0, baseTime.AddMinutes(2));

        // Act
        var recent = _database.GetRecentMetrics("test", 1000);

        // Assert - Should return all available data
        Assert.Equal(3, recent.Count);
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public void GetStatistics_Should_Return_Null_For_Unknown_Metric()
    {
        // Act
        var stats = _database.GetStatistics("unknown.metric");

        // Assert
        Assert.Null(stats);
    }

    [Fact]
    public void GetStatistics_Should_Calculate_Basic_Stats()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));
        _database.StoreMetric("test", 30.0, baseTime.AddMinutes(2));
        _database.StoreMetric("test", 40.0, baseTime.AddMinutes(3));
        _database.StoreMetric("test", 50.0, baseTime.AddMinutes(4));

        // Act
        var stats = _database.GetStatistics("test");

        // Assert
        Assert.NotNull(stats);
        Assert.Equal("test", stats.MetricName);
        Assert.Equal(5, stats.Count);
        Assert.Equal(30.0f, stats.Mean, 2);
        Assert.Equal(10.0f, stats.Min, 2);
        Assert.Equal(50.0f, stats.Max, 2);
        Assert.Equal(30.0f, stats.Median, 2);
    }

    [Fact]
    public void GetStatistics_Should_Calculate_Median_For_Even_Count()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));
        _database.StoreMetric("test", 30.0, baseTime.AddMinutes(2));
        _database.StoreMetric("test", 40.0, baseTime.AddMinutes(3));

        // Act
        var stats = _database.GetStatistics("test");

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(25.0f, stats.Median, 2); // (20 + 30) / 2
    }

    [Fact]
    public void GetStatistics_Should_Calculate_Percentiles()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        for (int i = 1; i <= 100; i++)
        {
            _database.StoreMetric("test", i, baseTime.AddMinutes(i));
        }

        // Act
        var stats = _database.GetStatistics("test");

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.P95 >= 90); // P95 should be around 95
        Assert.True(stats.P99 >= 98); // P99 should be around 99
    }

    [Fact]
    public void GetStatistics_Should_Calculate_StdDev()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime);
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(1));
        _database.StoreMetric("test", 30.0, baseTime.AddMinutes(2));

        // Act
        var stats = _database.GetStatistics("test");

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.StdDev > 0); // Should have some standard deviation
    }

    [Fact]
    public void GetStatistics_Should_Filter_By_Period()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime.AddMinutes(-10));
        _database.StoreMetric("test", 20.0, baseTime.AddMinutes(-5));
        _database.StoreMetric("test", 30.0, baseTime.AddMinutes(-1));

        // Act
        var stats = _database.GetStatistics("test", TimeSpan.FromMinutes(6));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(2, stats.Count); // Only last 2 within 6 minutes
    }

    [Fact]
    public void GetStatistics_Should_Throw_For_Empty_Metric_Name()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _database.GetStatistics(""));
    }

    [Fact]
    public void GetStatistics_Should_Handle_Very_Large_Dataset()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        for (int i = 0; i < 1000; i++)
        {
            _database.StoreMetric("large", i * 0.1, baseTime.AddMinutes(i));
        }

        // Act
        var stats = _database.GetStatistics("large");

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(1000, stats.Count);
        Assert.True(stats.Min >= 0);
        Assert.True(stats.Max < 100);
    }

    [Fact]
    public void GetStatistics_Should_Handle_Zero_Period()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _database.StoreMetric("test", 10.0, baseTime.AddMinutes(-10));
        _database.StoreMetric("test", 20.0, baseTime);

        // Act
        var stats = _database.GetStatistics("test", TimeSpan.Zero);

        // Assert - Should return null or empty stats for zero period
        Assert.Null(stats);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_Should_Clear_Data()
    {
        // Arrange
        using var db = TimeSeriesDatabase.Create(_logger);
        db.StoreMetric("test", 10.0, DateTime.UtcNow);

        // Act
        db.Dispose();

        // Assert - Should not throw when accessing after dispose
        var history = db.GetHistory("test");
        Assert.Empty(history);
    }

    [Fact]
    public void Dispose_Should_Be_Idempotent()
    {
        // Arrange
        using var db = TimeSeriesDatabase.Create(_logger);

        // Act
        db.Dispose();
        db.Dispose(); // Second dispose

        // Assert - Should not throw
    }

    [Fact]
    public void Dispose_Should_Log_Disposal()
    {
        // Arrange
        var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
        using var db = TimeSeriesDatabase.Create(loggerMock.Object);

        // Act
        db.Dispose();

        // Assert - Should have logged the disposal
        loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Time-series database disposed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void Dispose_Should_Dispose_Repository_If_IDisposable()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repositoryMock = new Moq.Mock<ITimeSeriesRepository>();
        repositoryMock.As<IDisposable>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repositoryMock.Object, forecastingService, anomalyDetectionService, statisticsService);

        // Act
        db.Dispose();

        // Assert - Repository Dispose should be called
        repositoryMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Should_Dispose_ForecastingService_If_IDisposable()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingServiceMock = new Moq.Mock<IForecastingService>();
        forecastingServiceMock.As<IDisposable>();
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingServiceMock.Object, anomalyDetectionService, statisticsService);

        // Act
        db.Dispose();

        // Assert - ForecastingService Dispose should be called
        forecastingServiceMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Should_Dispose_AnomalyDetectionService_If_IDisposable()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionServiceMock = new Moq.Mock<IAnomalyDetectionService>();
        anomalyDetectionServiceMock.As<IDisposable>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionServiceMock.Object, statisticsService);

        // Act
        db.Dispose();

        // Assert - AnomalyDetectionService Dispose should be called
        anomalyDetectionServiceMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Should_Dispose_StatisticsService_If_IDisposable()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsServiceMock = new Moq.Mock<ITimeSeriesStatisticsService>();
        statisticsServiceMock.As<IDisposable>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionService, statisticsServiceMock.Object);

        // Act
        db.Dispose();

        // Assert - StatisticsService Dispose should be called
        statisticsServiceMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    #endregion

    #region Forecasting Method Tests

    [Fact]
    public void GetForecastingMethod_Should_Delegate_To_ForecastingService()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingServiceMock = new Moq.Mock<IForecastingService>();
        forecastingServiceMock.Setup(x => x.GetForecastingMethod("test.metric")).Returns(ForecastingMethod.SSA);
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingServiceMock.Object, anomalyDetectionService, statisticsService);

        // Act
        var result = db.GetForecastingMethod("test.metric");

        // Assert
        Assert.Equal(ForecastingMethod.SSA, result);
        forecastingServiceMock.Verify(x => x.GetForecastingMethod("test.metric"), Times.Once);
    }

    [Fact]
    public void SetForecastingMethod_Should_Delegate_To_ForecastingService()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingServiceMock = new Moq.Mock<IForecastingService>();
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingServiceMock.Object, anomalyDetectionService, statisticsService);

        // Act
        db.SetForecastingMethod("test.metric", ForecastingMethod.ExponentialSmoothing);

        // Assert
        forecastingServiceMock.Verify(x => x.SetForecastingMethod("test.metric", ForecastingMethod.ExponentialSmoothing), Times.Once);
    }

    #endregion

    #region Anomaly Detection Edge Cases Tests

    [Fact]
    public void DetectAnomalies_Should_Delegate_With_Zero_LookbackPoints()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionServiceMock = new Moq.Mock<IAnomalyDetectionService>();
        anomalyDetectionServiceMock.Setup(x => x.DetectAnomalies("test.metric", 0))
            .Returns(new List<AnomalyDetectionResult>());
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionServiceMock.Object, statisticsService);

        // Act
        var result = db.DetectAnomalies("test.metric", 0);

        // Assert
        Assert.NotNull(result);
        anomalyDetectionServiceMock.Verify(x => x.DetectAnomalies("test.metric", 0), Times.Once);
    }

    [Fact]
    public void DetectAnomalies_Should_Delegate_With_Negative_LookbackPoints()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionServiceMock = new Moq.Mock<IAnomalyDetectionService>();
        anomalyDetectionServiceMock.Setup(x => x.DetectAnomalies("test.metric", -1))
            .Returns(new List<AnomalyDetectionResult>());
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionServiceMock.Object, statisticsService);

        // Act
        var result = db.DetectAnomalies("test.metric", -1);

        // Assert
        Assert.NotNull(result);
        anomalyDetectionServiceMock.Verify(x => x.DetectAnomalies("test.metric", -1), Times.Once);
    }

    [Fact]
    public void DetectAnomalies_Should_Delegate_With_Very_Large_LookbackPoints()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionServiceMock = new Moq.Mock<IAnomalyDetectionService>();
        anomalyDetectionServiceMock.Setup(x => x.DetectAnomalies("test.metric", 100000))
            .Returns(new List<AnomalyDetectionResult>());
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionServiceMock.Object, statisticsService);

        // Act
        var result = db.DetectAnomalies("test.metric", 100000);

        // Assert
        Assert.NotNull(result);
        anomalyDetectionServiceMock.Verify(x => x.DetectAnomalies("test.metric", 100000), Times.Once);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_Should_Delegate_With_Zero_LookbackPoints()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionServiceMock = new Moq.Mock<IAnomalyDetectionService>();
        anomalyDetectionServiceMock.Setup(x => x.DetectAnomaliesAsync("test.metric", 0))
            .ReturnsAsync(new List<AnomalyDetectionResult>());
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionServiceMock.Object, statisticsService);

        // Act
        var result = await db.DetectAnomaliesAsync("test.metric", 0);

        // Assert
        Assert.NotNull(result);
        anomalyDetectionServiceMock.Verify(x => x.DetectAnomaliesAsync("test.metric", 0), Times.Once);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_Should_Delegate_With_Negative_LookbackPoints()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionServiceMock = new Moq.Mock<IAnomalyDetectionService>();
        anomalyDetectionServiceMock.Setup(x => x.DetectAnomaliesAsync("test.metric", -50))
            .ReturnsAsync(new List<AnomalyDetectionResult>());
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionServiceMock.Object, statisticsService);

        // Act
        var result = await db.DetectAnomaliesAsync("test.metric", -50);

        // Assert
        Assert.NotNull(result);
        anomalyDetectionServiceMock.Verify(x => x.DetectAnomaliesAsync("test.metric", -50), Times.Once);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_Should_Delegate_With_Very_Large_LookbackPoints()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionServiceMock = new Moq.Mock<IAnomalyDetectionService>();
        anomalyDetectionServiceMock.Setup(x => x.DetectAnomaliesAsync("test.metric", int.MaxValue))
            .ReturnsAsync(new List<AnomalyDetectionResult>());
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionServiceMock.Object, statisticsService);

        // Act
        var result = await db.DetectAnomaliesAsync("test.metric", int.MaxValue);

        // Assert
        Assert.NotNull(result);
        anomalyDetectionServiceMock.Verify(x => x.DetectAnomaliesAsync("test.metric", int.MaxValue), Times.Once);
    }

    #endregion

    #region Dispose Repository Clear Tests

    [Fact]
    public void Dispose_Should_Call_Repository_Clear_Before_Disposing_Services()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repositoryMock = new Moq.Mock<ITimeSeriesRepository>();
        repositoryMock.As<IDisposable>();
        var forecastingServiceMock = new Moq.Mock<IForecastingService>();
        forecastingServiceMock.As<IDisposable>();
        var anomalyDetectionServiceMock = new Moq.Mock<IAnomalyDetectionService>();
        anomalyDetectionServiceMock.As<IDisposable>();
        var statisticsServiceMock = new Moq.Mock<ITimeSeriesStatisticsService>();
        statisticsServiceMock.As<IDisposable>();

        var db = new TimeSeriesDatabase(logger, repositoryMock.Object, forecastingServiceMock.Object,
            anomalyDetectionServiceMock.Object, statisticsServiceMock.Object);

        // Act
        db.Dispose();

        // Assert - Verify Clear is called before Dispose
        var callOrder = repositoryMock.Invocations.Select(i => i.Method.Name).ToList();
        Assert.Contains("Clear", callOrder);
        Assert.Contains("Dispose", callOrder);

        // Clear should be called before Dispose
        var clearIndex = callOrder.IndexOf("Clear");
        var disposeIndex = callOrder.IndexOf("Dispose");
        Assert.True(clearIndex < disposeIndex);

        // Verify all services are disposed
        forecastingServiceMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
        anomalyDetectionServiceMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
        statisticsServiceMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    #endregion

    #region Async Cancellation Tests

    [Fact]
    public async Task TrainForecastModelAsync_Should_Handle_Cancellation_Gracefully()
    {
        // Arrange
        var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingServiceMock = new Moq.Mock<IForecastingService>();
        forecastingServiceMock.Setup(x => x.TrainForecastModelAsync("test.metric", null))
            .ThrowsAsync(new OperationCanceledException());
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(loggerMock.Object, repository, forecastingServiceMock.Object,
            anomalyDetectionService, statisticsService);

        // Act & Assert - Should not throw, should log error
        await db.TrainForecastModelAsync("test.metric");

        // Verify error was logged
        loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error training forecast model for test.metric")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ForecastAsync_Should_Propagate_Cancellation()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingServiceMock = new Moq.Mock<IForecastingService>();
        forecastingServiceMock.Setup(x => x.ForecastAsync("test.metric", 12))
            .ThrowsAsync(new OperationCanceledException());
        var anomalyDetectionService = Mock.Of<IAnomalyDetectionService>();
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingServiceMock.Object,
            anomalyDetectionService, statisticsService);

        // Act & Assert - Should propagate the cancellation
        await Assert.ThrowsAsync<OperationCanceledException>(() => db.ForecastAsync("test.metric", 12));
    }

    [Fact]
    public async Task DetectAnomaliesAsync_Should_Propagate_Cancellation()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TimeSeriesDatabase>>();
        var repository = Mock.Of<ITimeSeriesRepository>();
        var forecastingService = Mock.Of<IForecastingService>();
        var anomalyDetectionServiceMock = new Moq.Mock<IAnomalyDetectionService>();
        anomalyDetectionServiceMock.Setup(x => x.DetectAnomaliesAsync("test.metric", 100))
            .ThrowsAsync(new OperationCanceledException());
        var statisticsService = Mock.Of<ITimeSeriesStatisticsService>();

        var db = new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionServiceMock.Object, statisticsService);

        // Act & Assert - Should propagate the cancellation
        await Assert.ThrowsAsync<OperationCanceledException>(() => db.DetectAnomaliesAsync("test.metric", 100));
    }

    #endregion
}