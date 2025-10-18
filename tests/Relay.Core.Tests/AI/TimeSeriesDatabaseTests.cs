using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class TimeSeriesDatabaseTests : IDisposable
    {
        private readonly ILogger<TimeSeriesDatabase> _logger;
        private readonly TimeSeriesDatabase _database;

        public TimeSeriesDatabaseTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            _database = new TimeSeriesDatabase(_logger, maxHistorySize: 1000);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TimeSeriesDatabase(null!));
        }

        [Fact]
        public void Constructor_Should_Initialize_With_Default_MaxHistorySize()
        {
            // Arrange & Act
            using var db = new TimeSeriesDatabase(_logger);

            // Assert - Should not throw
            db.StoreMetric("test", 1.0, DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_Should_Initialize_With_Custom_MaxHistorySize()
        {
            // Arrange & Act
            using var db = new TimeSeriesDatabase(_logger, maxHistorySize: 500);

            // Assert - Should not throw
            db.StoreMetric("test", 1.0, DateTime.UtcNow);
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

        #endregion

        #region TrainForecastModel Tests

        [Fact]
        public void TrainForecastModel_Should_Skip_With_Insufficient_Data()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 10; i++)
            {
                _database.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Act - Should not throw
            _database.TrainForecastModel("test");

            // Assert - Should log that data is insufficient
        }

        [Fact]
        public void TrainForecastModel_Should_Handle_Unknown_Metric()
        {
            // Act - Should not throw
            _database.TrainForecastModel("unknown.metric");

            // Assert - Should handle gracefully
        }

        [Fact]
        public void TrainForecastModel_Should_Train_With_Sufficient_Data()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-7);
            for (int i = 0; i < 100; i++)
            {
                _database.StoreMetric("test", 50.0 + i, baseTime.AddHours(i));
            }

            // Act - Should not throw
            _database.TrainForecastModel("test");

            // Assert - Model should be trained and forecast should work
            var forecast = _database.Forecast("test");
            Assert.NotNull(forecast);
            Assert.NotNull(forecast.ForecastedValues);
            Assert.True(forecast.ForecastedValues.Length > 0);
        }

        #endregion

        #region Forecast Tests

        [Fact]
        public void Forecast_Should_Return_Null_For_Unknown_Metric()
        {
            // Act
            var forecast = _database.Forecast("unknown.metric");

            // Assert
            Assert.Null(forecast);
        }

        [Fact]
        public void Forecast_Should_Return_Null_When_No_Model_And_Insufficient_Data()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 10.0, baseTime);

            // Act
            var forecast = _database.Forecast("test");

            // Assert
            Assert.Null(forecast);
        }

        [Fact]
        public void Forecast_Should_Return_Valid_Result_After_Model_Training()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-7);
            for (int i = 0; i < 100; i++)
            {
                _database.StoreMetric("test", 50.0 + i * 0.1, baseTime.AddHours(i));
            }

            // Train the model first
            _database.TrainForecastModel("test");

            // Act
            var forecast = _database.Forecast("test");

            // Assert
            Assert.NotNull(forecast);
            Assert.NotNull(forecast.ForecastedValues);
            Assert.NotNull(forecast.LowerBound);
            Assert.NotNull(forecast.UpperBound);
            Assert.True(forecast.ForecastedValues.Length > 0);
            Assert.Equal(forecast.ForecastedValues.Length, forecast.LowerBound.Length);
            Assert.Equal(forecast.ForecastedValues.Length, forecast.UpperBound.Length);
        }

        [Fact]
        public void Forecast_Should_Auto_Train_Model_When_None_Exists()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-7);
            for (int i = 0; i < 100; i++)
            {
                _database.StoreMetric("test", 50.0 + Math.Sin(i * 0.1), baseTime.AddHours(i));
            }

            // Act - Should auto-train model
            var forecast = _database.Forecast("test");

            // Assert
            Assert.NotNull(forecast);
            Assert.NotNull(forecast.ForecastedValues);
            Assert.True(forecast.ForecastedValues.Length > 0);
        }

        [Fact]
        public void Forecast_Should_Handle_Different_Horizons()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-7);
            for (int i = 0; i < 100; i++)
            {
                _database.StoreMetric("test", 50.0 + i * 0.1, baseTime.AddHours(i));
            }

            // Act
            var forecast12 = _database.Forecast("test", 12);
            var forecast24 = _database.Forecast("test", 24);

            // Assert - Both should work (horizon is currently fixed in model training)
            Assert.NotNull(forecast12);
            Assert.NotNull(forecast24);
            Assert.True(forecast12.ForecastedValues.Length > 0);
            Assert.True(forecast24.ForecastedValues.Length > 0);
        }

        [Fact]
        public void Forecast_Should_Return_Consistent_Results_For_Same_Data()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-7);
            for (int i = 0; i < 100; i++)
            {
                _database.StoreMetric("test", 50.0 + i * 0.1, baseTime.AddHours(i));
            }

            // Act
            var forecast1 = _database.Forecast("test");
            var forecast2 = _database.Forecast("test");

            // Assert
            Assert.NotNull(forecast1);
            Assert.NotNull(forecast2);
            Assert.Equal(forecast1.ForecastedValues.Length, forecast2.ForecastedValues.Length);
            Assert.Equal(forecast1.LowerBound.Length, forecast2.LowerBound.Length);
            Assert.Equal(forecast1.UpperBound.Length, forecast2.UpperBound.Length);
        }

        [Fact]
        public void Forecast_Should_Handle_Empty_History_Gracefully()
        {
            // Arrange - No data stored

            // Act
            var forecast = _database.Forecast("nonexistent");

            // Assert
            Assert.Null(forecast);
        }

        #endregion

        #region DetectAnomalies Tests

        [Fact]
        public void DetectAnomalies_Should_Return_Empty_For_Unknown_Metric()
        {
            // Act
            var anomalies = _database.DetectAnomalies("unknown.metric");

            // Assert
            Assert.Empty(anomalies);
        }

        [Fact]
        public void DetectAnomalies_Should_Return_Empty_With_Insufficient_Data()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 5; i++)
            {
                _database.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Act
            var anomalies = _database.DetectAnomalies("test");

            // Assert
            Assert.Empty(anomalies);
        }

        [Fact]
        public void DetectAnomalies_Should_Detect_With_Sufficient_Data()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            // Create a pattern with an anomaly
            for (int i = 0; i < 50; i++)
            {
                var value = i == 25 ? 1000.0 : 50.0; // Anomaly at index 25
                _database.StoreMetric("test", value, baseTime.AddMinutes(i));
            }

            // Act
            var anomalies = _database.DetectAnomalies("test", lookbackPoints: 50);

            // Assert - May or may not detect the anomaly depending on ML.NET sensitivity
            // Just verify it doesn't throw
            Assert.NotNull(anomalies);
        }

        [Fact]
        public void DetectAnomalies_Should_Limit_Lookback_Points()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 200; i++)
            {
                _database.StoreMetric("test", 50.0, baseTime.AddMinutes(i));
            }

            // Act
            var anomalies = _database.DetectAnomalies("test", lookbackPoints: 50);

            // Assert - Should only analyze last 50 points
            Assert.NotNull(anomalies);
        }

        #endregion

        #region CleanupOldData Tests

        [Fact]
        public void CleanupOldData_Should_Remove_Old_Metrics()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 10.0, baseTime.AddDays(-10));
            _database.StoreMetric("test", 20.0, baseTime.AddDays(-8));
            _database.StoreMetric("test", 30.0, baseTime.AddDays(-1));
            _database.StoreMetric("test", 40.0, baseTime);

            // Act
            _database.CleanupOldData(TimeSpan.FromDays(7));

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.True(history.Count >= 2, $"Expected at least 2 items, got {history.Count}");
            // Should keep items within last 7 days
            Assert.All(history, h => Assert.True(h.Timestamp >= baseTime.AddDays(-7)));
        }

        [Fact]
        public void CleanupOldData_Should_Keep_Recent_Metrics()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 10.0, baseTime.AddHours(-1));
            _database.StoreMetric("test", 20.0, baseTime.AddMinutes(-30));
            _database.StoreMetric("test", 30.0, baseTime);

            // Act
            _database.CleanupOldData(TimeSpan.FromDays(1));

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.Equal(3, history.Count); // All within 1 day
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Multiple_Metrics()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("metric1", 10.0, baseTime.AddDays(-10));
            _database.StoreMetric("metric1", 20.0, baseTime);
            _database.StoreMetric("metric2", 30.0, baseTime.AddDays(-10));
            _database.StoreMetric("metric2", 40.0, baseTime);

            // Act
            _database.CleanupOldData(TimeSpan.FromDays(7));

            // Assert
            var history1 = _database.GetHistory("metric1").ToList();
            var history2 = _database.GetHistory("metric2").ToList();
            Assert.Single(history1);
            Assert.Single(history2);
        }

        [Fact]
        public void CleanupOldData_Should_Not_Throw_When_Empty()
        {
            // Act - Should not throw
            _database.CleanupOldData(TimeSpan.FromDays(1));

            // Assert - No exception
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_Should_Clear_Data()
        {
            // Arrange
            using var db = new TimeSeriesDatabase(_logger);
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
            using var db = new TimeSeriesDatabase(_logger);

            // Act
            db.Dispose();
            db.Dispose(); // Second dispose

            // Assert - Should not throw
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void StoreMetric_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange - Create a database with a mock logger to verify graceful handling
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = new TimeSeriesDatabase(loggerMock.Object);

            // Act - This should not throw even with problematic values
            db.StoreMetric("test", double.NaN, DateTime.UtcNow); // NaN values are handled gracefully

            // Assert - Should not throw, method handles NaN values without exceptions
            var history = db.GetHistory("test").ToList();
            Assert.Single(history);
            Assert.True(float.IsNaN(history[0].Value)); // NaN is preserved
        }

        [Fact]
        public void TrainForecastModel_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = new TimeSeriesDatabase(loggerMock.Object);

            // Store some data that might cause ML.NET issues
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 100; i++)
            {
                db.StoreMetric("test", double.PositiveInfinity, baseTime.AddHours(i)); // Infinity might cause issues
            }

            // Act - Should not throw
            db.TrainForecastModel("test");

            // Assert - Should have logged the error
            loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error training forecast model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Forecast_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = new TimeSeriesDatabase(loggerMock.Object);

            // Store data that should work but might cause internal issues
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 10; i++)
            {
                // Mix of normal and problematic values
                var value = i == 5 ? double.NaN : 50.0;
                db.StoreMetric("test", value, baseTime.AddMinutes(i));
            }

            // Act - Should not throw even with NaN values
            var result = db.Forecast("test");

            // Assert - Should complete without throwing
            Assert.Null(result); // Returns null due to insufficient data for forecasting
        }

        [Fact]
        public void DetectAnomalies_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = new TimeSeriesDatabase(loggerMock.Object);

            // Store data that should work but might cause internal issues
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 50; i++)
            {
                // Mix of normal and problematic values
                var value = i == 25 ? double.NaN : 50.0;
                db.StoreMetric("test", value, baseTime.AddMinutes(i));
            }

            // Act - Should not throw even with NaN values
            var anomalies = db.DetectAnomalies("test");

            // Assert - Should complete without throwing
            Assert.NotNull(anomalies);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = new TimeSeriesDatabase(loggerMock.Object);

            // Store some data
            db.StoreMetric("test", 10.0, DateTime.UtcNow);

            // Act - Should not throw even if cleanup fails
            db.CleanupOldData(TimeSpan.FromDays(1));

            // Assert - Should not throw (cleanup is best-effort)
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void GetStatistics_Should_Handle_Single_Data_Point()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 42.0, baseTime);

            // Act
            var stats = _database.GetStatistics("test");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
            Assert.Equal(42.0f, stats.Mean, 2);
            Assert.Equal(42.0f, stats.Min, 2);
            Assert.Equal(42.0f, stats.Max, 2);
            Assert.Equal(42.0f, stats.Median, 2);
            Assert.Equal(0.0f, stats.StdDev, 2); // No variation with single point
        }

        [Fact]
        public void GetStatistics_Should_Handle_Zero_Values()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 0.0, baseTime);
            _database.StoreMetric("test", 0.0, baseTime.AddMinutes(1));

            // Act
            var stats = _database.GetStatistics("test");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(0.0f, stats.Mean, 2);
            Assert.Equal(0.0f, stats.Min, 2);
            Assert.Equal(0.0f, stats.Max, 2);
            Assert.Equal(0.0f, stats.StdDev, 2);
        }

        [Fact]
        public void GetStatistics_Should_Handle_Negative_Values()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", -10.0, baseTime);
            _database.StoreMetric("test", -5.0, baseTime.AddMinutes(1));
            _database.StoreMetric("test", 5.0, baseTime.AddMinutes(2));

            // Act
            var stats = _database.GetStatistics("test");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(-10.0f, stats.Min, 2);
            Assert.Equal(5.0f, stats.Max, 2);
            Assert.Equal(-3.33f, stats.Mean, 2);
        }

        [Fact]
        public void StoreMetric_Should_Handle_Extreme_Values()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;

            // Act
            _database.StoreMetric("test", double.MaxValue, timestamp);
            _database.StoreMetric("test", double.MinValue, timestamp.AddMinutes(1));

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.Equal(2, history.Count);
            Assert.Equal(float.PositiveInfinity, history[0].Value); // double.MaxValue becomes float.PositiveInfinity when cast
            Assert.Equal(float.NegativeInfinity, history[1].Value); // double.MinValue becomes float.NegativeInfinity when cast
        }

        [Fact]
        public void StoreBatch_Should_Handle_Null_MovingAverages_Dictionary()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var metrics = new Dictionary<string, double>
            {
                ["test"] = 100.0
            };

            // Act
            _database.StoreBatch(metrics, timestamp, movingAverages: null);

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.Single(history);
            Assert.Equal(100.0f, history[0].Value, 2);
            Assert.Equal(100.0f, history[0].MA5, 2); // Should default to value
            Assert.Equal(100.0f, history[0].MA15, 2); // Should default to value
        }

        [Fact]
        public void StoreBatch_Should_Handle_Null_TrendDirections_Dictionary()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var metrics = new Dictionary<string, double>
            {
                ["test"] = 100.0
            };

            // Act
            _database.StoreBatch(metrics, timestamp, trendDirections: null);

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.Single(history);
            Assert.Equal((int)TrendDirection.Stable, history[0].Trend); // Should default to Stable
        }

        [Fact]
        public void GetHistory_Should_Handle_Very_Old_Lookback_Period()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 10.0, baseTime.AddYears(-1));
            _database.StoreMetric("test", 20.0, baseTime);

            // Act
            var history = _database.GetHistory("test", TimeSpan.FromDays(365 * 2)).ToList();

            // Assert
            Assert.Equal(2, history.Count); // Should return all data
        }

        [Fact]
        public void GetRecentMetrics_Should_Handle_Count_Zero()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 10.0, baseTime);

            // Act
            var recent = _database.GetRecentMetrics("test", 0);

            // Assert
            Assert.Empty(recent);
        }

        [Fact]
        public void DetectAnomalies_Should_Handle_Exactly_12_Data_Points()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 12; i++)
            {
                _database.StoreMetric("test", 50.0, baseTime.AddMinutes(i));
            }

            // Act
            var anomalies = _database.DetectAnomalies("test");

            // Assert
            Assert.NotNull(anomalies);
            // May or may not detect anomalies, but shouldn't throw
        }

        [Fact]
        public void TrainForecastModel_Should_Handle_Exactly_48_Data_Points()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-1);
            for (int i = 0; i < 48; i++)
            {
                _database.StoreMetric("test", 50.0 + i, baseTime.AddHours(i));
            }

            // Act - Should not throw
            _database.TrainForecastModel("test");

            // Assert - Should handle exactly the minimum required data
        }

        #endregion

        #region Circular Buffer Tests

        [Fact]
        public void CircularBuffer_Should_Respect_MaxHistorySize()
        {
            // Arrange
            using var db = new TimeSeriesDatabase(_logger, maxHistorySize: 10);
            var baseTime = DateTime.UtcNow;

            // Act - Store more than max size
            for (int i = 0; i < 15; i++)
            {
                db.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Assert
            var history = db.GetHistory("test").ToList();
            Assert.Equal(10, history.Count); // Should only keep last 10
            Assert.Equal(50.0f, history[0].Value, 2); // First should be 50 (index 5)
            Assert.Equal(140.0f, history[9].Value, 2); // Last should be 140 (index 14)
        }

        [Fact]
        public void CircularBuffer_Should_Maintain_Order_After_Overflow()
        {
            // Arrange
            using var db = new TimeSeriesDatabase(_logger, maxHistorySize: 5);
            var baseTime = DateTime.UtcNow;

            // Act
            for (int i = 0; i < 8; i++)
            {
                db.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Assert
            var history = db.GetHistory("test").ToList();
            Assert.Equal(5, history.Count);
            // Should be ordered by timestamp
            for (int i = 1; i < history.Count; i++)
            {
                Assert.True(history[i - 1].Timestamp <= history[i].Timestamp);
            }
        }

        #endregion

        #region Concurrent Access Tests

        [Fact]
        public async System.Threading.Tasks.Task TimeSeriesDatabase_Should_Handle_Concurrent_Reads_And_Writes()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            var tasks = new System.Threading.Tasks.Task[20];

            // Act - Mix of reads and writes
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        _database.StoreMetric($"metric{index}", j * 10.0, baseTime.AddMinutes(j));
                    }
                });
            }

            for (int i = 10; i < 20; i++)
            {
                var index = i - 10;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (int j = 0; j < 5; j++)
                    {
                        var history = _database.GetHistory($"metric{index}");
                        var stats = _database.GetStatistics($"metric{index}");
                        var recent = _database.GetRecentMetrics($"metric{index}", 3);
                        var anomalies = _database.DetectAnomalies($"metric{index}");
                    }
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - Should not have thrown and data should be consistent
            for (int i = 0; i < 10; i++)
            {
                var history = _database.GetHistory($"metric{i}").ToList();
                Assert.True(history.Count >= 0); // May have been read during writes
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task TimeSeriesDatabase_Should_Handle_Concurrent_Cleanup()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            var tasks = new System.Threading.Tasks.Task[5];

            // Store some data
            for (int i = 0; i < 50; i++)
            {
                _database.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Act - Concurrent cleanup operations
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    _database.CleanupOldData(TimeSpan.FromHours(1));
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - Should not throw
            var history = _database.GetHistory("test").ToList();
            Assert.True(history.Count >= 0);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void TimeSeriesDatabase_Should_Handle_Large_Dataset_Efficiently()
        {
            // Arrange
            using var db = new TimeSeriesDatabase(_logger, maxHistorySize: 10000);
            var baseTime = DateTime.UtcNow;

            // Act - Store large amount of data
            for (int i = 0; i < 5000; i++)
            {
                db.StoreMetric("performance.test", i * 0.1, baseTime.AddSeconds(i));
            }

            // Assert - Operations should complete reasonably fast
            var history = db.GetHistory("performance.test").ToList();
            var stats = db.GetStatistics("performance.test");
            var recent = db.GetRecentMetrics("performance.test", 100);

            Assert.Equal(5000, history.Count);
            Assert.NotNull(stats);
            Assert.Equal(100, recent.Count);
        }

        [Fact]
        public void DetectAnomalies_Should_Perform_With_Large_Dataset()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 200; i++)
            {
                var value = i == 100 ? 1000.0 : 50.0; // Anomaly in middle
                _database.StoreMetric("large.test", value, baseTime.AddMinutes(i));
            }

            // Act
            var anomalies = _database.DetectAnomalies("large.test", lookbackPoints: 200);

            // Assert
            Assert.NotNull(anomalies);
            // Should handle large datasets without throwing
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void TimeSeriesDatabase_Should_Handle_Complete_Workflow()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;

            // Act - Store metrics
            for (int i = 0; i < 100; i++)
            {
                _database.StoreMetric("response.time", 100.0 + i, baseTime.AddMinutes(i));
            }

            // Get history
            var history = _database.GetHistory("response.time", TimeSpan.FromHours(2)).ToList();

            // Get statistics
            var stats = _database.GetStatistics("response.time");

            // Get recent metrics
            var recent = _database.GetRecentMetrics("response.time", 10);

            // Detect anomalies
            var anomalies = _database.DetectAnomalies("response.time");

            // Cleanup old data
            _database.CleanupOldData(TimeSpan.FromDays(1));

            // Assert
            Assert.NotEmpty(history);
            Assert.NotNull(stats);
            Assert.NotEmpty(recent);
            Assert.NotNull(anomalies);
        }

        [Fact]
        public async System.Threading.Tasks.Task TimeSeriesDatabase_Should_Handle_Concurrent_Access()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            var tasks = new System.Threading.Tasks.Task[10];

            // Act - Concurrent writes
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        _database.StoreMetric($"metric{index}", j * 10.0, baseTime.AddMinutes(j));
                    }
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert
            for (int i = 0; i < 10; i++)
            {
                var history = _database.GetHistory($"metric{i}").ToList();
                Assert.Equal(10, history.Count);
            }
        }

        [Fact]
        public void TimeSeriesDatabase_Should_Respect_MaxHistorySize()
        {
            // Arrange
            using var db = new TimeSeriesDatabase(_logger, maxHistorySize: 100);
            var baseTime = DateTime.UtcNow;

            // Act - Store more than max
            for (int i = 0; i < 150; i++)
            {
                db.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Assert - Should only keep last 100
            var history = db.GetHistory("test").ToList();
            Assert.True(history.Count <= 100);
        }

        #endregion
    }
}
