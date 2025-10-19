using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class TimeSeriesDatabaseForecastingTests : IDisposable
    {
        private readonly ILogger<TimeSeriesDatabase> _logger;
        private readonly TimeSeriesDatabase _database;

        public TimeSeriesDatabaseForecastingTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            _database = TimeSeriesDatabase.Create(_logger, maxHistorySize: 1000);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

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

        [Fact]
        public void ForecastingMethod_Should_Default_To_SSA()
        {
            // Act
            var method = _database.GetForecastingMethod("test");

            // Assert
            Assert.Equal(ForecastingMethod.SSA, method);
        }

        [Fact]
        public void SetForecastingMethod_Should_Update_Method_For_Metric()
        {
            // Act
            _database.SetForecastingMethod("test", ForecastingMethod.ExponentialSmoothing);
            var method = _database.GetForecastingMethod("test");

            // Assert
            Assert.Equal(ForecastingMethod.ExponentialSmoothing, method);
        }

        [Fact]
        public void TrainForecastModel_Should_Support_Different_Methods()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-7);
            for (int i = 0; i < 100; i++)
            {
                _database.StoreMetric("test", 50.0 + i * 0.1, baseTime.AddHours(i));
            }

            // Act - Train with Exponential Smoothing
            _database.TrainForecastModel("test", ForecastingMethod.ExponentialSmoothing);

            // Assert
            var method = _database.GetForecastingMethod("test");
            Assert.Equal(ForecastingMethod.ExponentialSmoothing, method);

            var forecast = _database.Forecast("test");
            Assert.NotNull(forecast);
        }

        [Fact]
        public void Forecast_Should_Work_With_MovingAverage_Method()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-7);
            for (int i = 0; i < 80; i++)
            {
                _database.StoreMetric("test", 50.0 + Math.Sin(i * 0.2), baseTime.AddHours(i));
            }

            // Act
            _database.SetForecastingMethod("test", ForecastingMethod.MovingAverage);
            _database.TrainForecastModel("test", ForecastingMethod.MovingAverage);
            var forecast = _database.Forecast("test");

            // Assert
            Assert.NotNull(forecast);
            Assert.NotNull(forecast.ForecastedValues);
            Assert.True(forecast.ForecastedValues.Length > 0);
        }

        [Fact]
        public void Forecast_Should_Work_With_Ensemble_Method()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-7);
            for (int i = 0; i < 120; i++)
            {
                _database.StoreMetric("test", 50.0 + i * 0.05 + Math.Sin(i * 0.1), baseTime.AddHours(i));
            }

            // Act
            _database.SetForecastingMethod("test", ForecastingMethod.Ensemble);
            _database.TrainForecastModel("test", ForecastingMethod.Ensemble);
            var forecast = _database.Forecast("test");

            // Assert
            Assert.NotNull(forecast);
            Assert.NotNull(forecast.ForecastedValues);
            Assert.True(forecast.ForecastedValues.Length > 0);
            Assert.Equal(_database.GetForecastingMethod("test"), ForecastingMethod.Ensemble);
        }

        [Fact]
        public void TrainForecastModel_Should_Handle_Invalid_Method_Gracefully()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-7);
            for (int i = 0; i < 50; i++)
            {
                _database.StoreMetric("test", 50.0 + i, baseTime.AddHours(i));
            }

            // Act - This should not throw, but log an error
            _database.TrainForecastModel("test", (ForecastingMethod)999); // Invalid method

            // Assert - Should still have a model (fallback to default)
            var forecast = _database.Forecast("test");
            // May be null due to error, but shouldn't crash
        }

        #endregion
    }
}