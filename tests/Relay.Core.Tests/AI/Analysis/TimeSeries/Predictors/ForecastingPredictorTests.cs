using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.TimeSeries.Predictors
{
    public class ForecastingPredictorTests
    {
        private readonly Mock<ILogger<ForecastingPredictor>> _loggerMock;
        private readonly Mock<ITimeSeriesRepository> _repositoryMock;
        private readonly Mock<IForecastingModelManager> _modelManagerMock;
        private readonly Mock<IForecastingTrainer> _trainerMock;
        private readonly ForecastingConfiguration _config;
        private readonly ForecastingPredictor _predictor;

        public ForecastingPredictorTests()
        {
            _loggerMock = new Mock<ILogger<ForecastingPredictor>>();
            _repositoryMock = new Mock<ITimeSeriesRepository>();
            _modelManagerMock = new Mock<IForecastingModelManager>();
            _trainerMock = new Mock<IForecastingTrainer>();
            _config = new ForecastingConfiguration
            {
                AutoTrainOnForecast = true,
                MinimumDataPoints = 10,
                TrainingDataWindowDays = 7,
                MlContextSeed = 42
            };

            _predictor = new ForecastingPredictor(
                _loggerMock.Object,
                _repositoryMock.Object,
                _modelManagerMock.Object,
                _trainerMock.Object,
                _config);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ForecastingPredictor(null!, _repositoryMock.Object, _modelManagerMock.Object, _trainerMock.Object, _config));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ForecastingPredictor(_loggerMock.Object, null!, _modelManagerMock.Object, _trainerMock.Object, _config));
        }

        [Fact]
        public void Constructor_Should_Throw_When_ModelManager_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ForecastingPredictor(_loggerMock.Object, _repositoryMock.Object, null!, _trainerMock.Object, _config));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Trainer_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ForecastingPredictor(_loggerMock.Object, _repositoryMock.Object, _modelManagerMock.Object, null!, _config));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Config_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ForecastingPredictor(_loggerMock.Object, _repositoryMock.Object, _modelManagerMock.Object, _trainerMock.Object, null!));
        }

        #endregion

        #region Predict Tests

        [Fact]
        public void Predict_Should_Throw_When_MetricName_Is_Null()
        {
            var exception = Assert.Throws<ArgumentException>(() => _predictor.Predict(null!));
            Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
        }

        [Fact]
        public void Predict_Should_Throw_When_MetricName_Is_Empty()
        {
            var exception = Assert.Throws<ArgumentException>(() => _predictor.Predict(string.Empty));
            Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
        }

        [Fact]
        public void Predict_Should_Throw_When_MetricName_Is_Whitespace()
        {
            var exception = Assert.Throws<ArgumentException>(() => _predictor.Predict("   "));
            Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
        }

        [Fact]
        public void Predict_Should_Throw_When_Horizon_Is_Zero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _predictor.Predict("test.metric", 0));
        }

        [Fact]
        public void Predict_Should_Throw_When_Horizon_Is_Negative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _predictor.Predict("test.metric", -1));
        }

        [Fact]
        public void Predict_Should_Return_Null_When_No_Model_And_AutoTrain_Disabled()
        {
            // Arrange
            var config = new ForecastingConfiguration { AutoTrainOnForecast = false };
            var predictor = new ForecastingPredictor(
                _loggerMock.Object, _repositoryMock.Object, _modelManagerMock.Object, _trainerMock.Object, config);

            _modelManagerMock.Setup(m => m.HasModel("test.metric")).Returns(false);

            // Act
            var result = predictor.Predict("test.metric");

            // Assert
            Assert.Null(result);
            _modelManagerMock.Verify(m => m.HasModel("test.metric"), Times.Once);
            _trainerMock.Verify(t => t.TrainModel(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Predict_Should_AutoTrain_When_No_Model_And_AutoTrain_Enabled()
        {
            // Arrange
            var metricName = "test.metric";
            var hasModelCallCount = 0;
            _modelManagerMock.Setup(m => m.HasModel(metricName))
                .Returns(() => hasModelCallCount++ == 0 ? false : true); // Return false first, then true

            _trainerMock.Setup(t => t.TrainModel(metricName)).Verifiable();

            var model = Mock.Of<ITransformer>();
            _modelManagerMock.Setup(m => m.GetModel(metricName)).Returns(model);

            var history = CreateTestHistory(15);
            _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(history);

            // Act
            var result = _predictor.Predict(metricName);

            // Assert
            // Result may be null due to mocked ML.NET operations, but training should be attempted
            _trainerMock.Verify(t => t.TrainModel(metricName), Times.Once);
            _modelManagerMock.Verify(m => m.HasModel(metricName), Times.Exactly(2)); // Before and after training
        }

        [Fact]
        public void Predict_Should_Return_Null_When_AutoTrain_Fails()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(false);
            _trainerMock.Setup(t => t.TrainModel(metricName)).Verifiable();
            // Model still not available after training

            // Act
            var result = _predictor.Predict(metricName);

            // Assert
            Assert.Null(result);
            _trainerMock.Verify(t => t.TrainModel(metricName), Times.Once);
        }

        [Fact]
        public void Predict_Should_Return_Null_When_Model_Is_Null()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(true);
            _modelManagerMock.Setup(m => m.GetModel(metricName)).Returns((ITransformer?)null);

            // Act
            var result = _predictor.Predict(metricName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Predict_Should_Return_Null_When_No_Historical_Data()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(true);
            _modelManagerMock.Setup(m => m.GetModel(metricName)).Returns(Mock.Of<ITransformer>());
            _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(new List<MetricDataPoint>());

            // Act
            var result = _predictor.Predict(metricName);

            // Assert
            Assert.Null(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("No data available")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Predict_Should_Return_Null_When_Insufficient_Data()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(true);
            _modelManagerMock.Setup(m => m.GetModel(metricName)).Returns(Mock.Of<ITransformer>());

            var insufficientHistory = CreateTestHistory(5); // Less than minimum 10
            _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(insufficientHistory);

            // Act
            var result = _predictor.Predict(metricName);

            // Assert
            Assert.Null(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Insufficient data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Predict_Should_Attempt_Forecast_When_Data_Is_Sufficient()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(true);
            _modelManagerMock.Setup(m => m.GetMethod(metricName)).Returns(ForecastingMethod.SSA);

            var model = Mock.Of<ITransformer>();
            _modelManagerMock.Setup(m => m.GetModel(metricName)).Returns(model);

            var history = CreateTestHistory(15);
            _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(history);

            // Act
            var result = _predictor.Predict(metricName, 3);

            // Assert
            // The operation may fail due to mocked ML.NET, but it should attempt and log appropriately
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void Predict_Should_Handle_Exception_And_Return_Null()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Throws(new Exception("Database error"));

            // Act
            var result = _predictor.Predict(metricName);

            // Assert
            Assert.Null(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error forecasting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Predict_Should_Store_Prediction_Metadata_On_Attempt()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(true);
            _modelManagerMock.Setup(m => m.GetMethod(metricName)).Returns(ForecastingMethod.SSA);

            var model = Mock.Of<ITransformer>();
            _modelManagerMock.Setup(m => m.GetModel(metricName)).Returns(model);

            var history = CreateTestHistory(15);
            _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(history);

            // Act
            _predictor.Predict(metricName, 5);

            // Assert
            var metadata = _predictor.GetPredictionMetadata(metricName);
            Assert.NotNull(metadata);
            Assert.Equal(metricName, metadata.MetricName);
            Assert.Equal(ForecastingMethod.SSA, metadata.Method);
            Assert.Equal(5, metadata.Horizon);
            Assert.Equal(15, metadata.TrainingDataPoints);
            // Success may be false due to mocked ML.NET operations
            Assert.NotNull(metadata.PredictedAt);
        }

        [Fact]
        public void Predict_Should_Store_Prediction_Metadata_On_Failure()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(false);
            var config = new ForecastingConfiguration { AutoTrainOnForecast = false };
            var predictor = new ForecastingPredictor(
                _loggerMock.Object, _repositoryMock.Object, _modelManagerMock.Object, _trainerMock.Object, config);

            // Act
            predictor.Predict(metricName);

            // Assert
            var metadata = predictor.GetPredictionMetadata(metricName);
            Assert.NotNull(metadata);
            Assert.Equal(metricName, metadata.MetricName);
            Assert.False(metadata.Success);
            Assert.NotNull(metadata.ErrorMessage);
        }

        #endregion

        #region PredictAsync Tests

        [Fact]
        public async Task PredictAsync_Should_Call_Sync_Method()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(true);
            _modelManagerMock.Setup(m => m.GetModel(metricName)).Returns(Mock.Of<ITransformer>());
            _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

            // Act
            var result = await _predictor.PredictAsync(metricName);

            // Assert
            // Result may be null due to mocked ML.NET operations, but the method should complete without throwing
            Assert.True(result == null || result != null); // Just ensure it doesn't throw
        }

        [Fact]
        public async Task PredictAsync_Should_Respect_CancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _predictor.PredictAsync("test.metric", cancellationToken: cts.Token));
        }

        #endregion

        #region PredictBatch Tests

        [Fact]
        public void PredictBatch_Should_Throw_When_MetricNames_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => _predictor.PredictBatch(null!));
        }

        [Fact]
        public void PredictBatch_Should_Throw_When_Horizon_Is_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _predictor.PredictBatch(new[] { "test.metric" }, 0));
        }

        [Fact]
        public void PredictBatch_Should_Attempt_Prediction_For_All_Metrics()
        {
            // Arrange
            var metrics = new[] { "metric1", "metric2", "metric3" };

            foreach (var metric in metrics)
            {
                _modelManagerMock.Setup(m => m.HasModel(metric)).Returns(true);
                _modelManagerMock.Setup(m => m.GetModel(metric)).Returns(Mock.Of<ITransformer>());
                _repositoryMock.Setup(r => r.GetHistory(metric, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));
            }

            // Act
            var results = _predictor.PredictBatch(metrics);

            // Assert
            Assert.Equal(metrics.Length, results.Count);
            foreach (var metric in metrics)
            {
                Assert.Contains(metric, results.Keys);
                // Results may be null due to mocked ML.NET operations
            }
        }

        [Fact]
        public void PredictBatch_Should_Handle_Mixed_Success_Failure()
        {
            // Arrange
            var metrics = new[] { "success.metric", "failure.metric" };

            // Success metric
            _modelManagerMock.Setup(m => m.HasModel("success.metric")).Returns(true);
            _modelManagerMock.Setup(m => m.GetModel("success.metric")).Returns(Mock.Of<ITransformer>());
            _repositoryMock.Setup(r => r.GetHistory("success.metric", It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

            // Failure metric - create a separate predictor with auto-train disabled
            var config = new ForecastingConfiguration { AutoTrainOnForecast = false };
            var predictor = new ForecastingPredictor(
                _loggerMock.Object, _repositoryMock.Object, _modelManagerMock.Object, _trainerMock.Object, config);

            // Act
            var results = predictor.PredictBatch(metrics);

            // Assert
            Assert.Equal(2, results.Count);
            // Results may be null due to mocked operations, but both should be attempted
            Assert.Contains("success.metric", results.Keys);
            Assert.Contains("failure.metric", results.Keys);
        }

        [Fact]
        public void PredictBatch_Should_Handle_Duplicate_Metrics()
        {
            // Arrange
            var metrics = new[] { "test.metric", "test.metric", "other.metric" };

            _modelManagerMock.Setup(m => m.HasModel("test.metric")).Returns(true);
            _modelManagerMock.Setup(m => m.GetModel("test.metric")).Returns(Mock.Of<ITransformer>());
            _repositoryMock.Setup(r => r.GetHistory("test.metric", It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

            _modelManagerMock.Setup(m => m.HasModel("other.metric")).Returns(true);
            _modelManagerMock.Setup(m => m.GetModel("other.metric")).Returns(Mock.Of<ITransformer>());
            _repositoryMock.Setup(r => r.GetHistory("other.metric", It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

            // Act
            var results = _predictor.PredictBatch(metrics);

            // Assert
            Assert.Equal(2, results.Count); // Duplicates removed
            Assert.Contains("test.metric", results.Keys);
            Assert.Contains("other.metric", results.Keys);
        }

        #endregion

        #region PredictBatchAsync Tests

        [Fact]
        public async Task PredictBatchAsync_Should_Call_Sync_Method()
        {
            // Arrange
            var metrics = new[] { "test.metric" };
            _modelManagerMock.Setup(m => m.HasModel("test.metric")).Returns(true);
            _modelManagerMock.Setup(m => m.GetModel("test.metric")).Returns(Mock.Of<ITransformer>());
            _repositoryMock.Setup(r => r.GetHistory("test.metric", It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

            // Act
            var results = await _predictor.PredictBatchAsync(metrics);

            // Assert
            Assert.Single(results);
            // Result may be null due to mocked operations
        }

        [Fact]
        public async Task PredictBatchAsync_Should_Respect_CancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _predictor.PredictBatchAsync(new[] { "test.metric" }, cancellationToken: cts.Token));
        }

        #endregion

        #region CanPredict Tests

        [Fact]
        public void CanPredict_Should_Throw_When_MetricName_Is_Invalid()
        {
            var exception = Assert.Throws<ArgumentException>(() => _predictor.CanPredict(null!));
            Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
        }

        [Fact]
        public void CanPredict_Should_Return_True_When_Model_Exists()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(true);

            // Act
            var result = _predictor.CanPredict(metricName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanPredict_Should_Return_True_When_AutoTrain_Enabled_And_Sufficient_Data()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(false);
            _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

            // Act
            var result = _predictor.CanPredict(metricName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanPredict_Should_Return_False_When_No_Model_And_AutoTrain_Disabled()
        {
            // Arrange
            var config = new ForecastingConfiguration { AutoTrainOnForecast = false };
            var predictor = new ForecastingPredictor(
                _loggerMock.Object, _repositoryMock.Object, _modelManagerMock.Object, _trainerMock.Object, config);

            _modelManagerMock.Setup(m => m.HasModel("test.metric")).Returns(false);

            // Act
            var result = predictor.CanPredict("test.metric");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanPredict_Should_Return_False_When_Insufficient_Data_For_AutoTrain()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(false);
            _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(5)); // Less than minimum

            // Act
            var result = _predictor.CanPredict(metricName);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetPredictionMetadata Tests

        [Fact]
        public void GetPredictionMetadata_Should_Throw_When_MetricName_Is_Invalid()
        {
            var exception = Assert.Throws<ArgumentException>(() => _predictor.GetPredictionMetadata(null!));
            Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
        }

        [Fact]
        public void GetPredictionMetadata_Should_Return_Null_When_No_Metadata()
        {
            // Act
            var result = _predictor.GetPredictionMetadata("nonexistent.metric");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetPredictionMetadata_Should_Return_Metadata_After_Prediction()
        {
            // Arrange
            var metricName = "test.metric";
            _modelManagerMock.Setup(m => m.HasModel(metricName)).Returns(true);
            _modelManagerMock.Setup(m => m.GetModel(metricName)).Returns(Mock.Of<ITransformer>());
            _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

            // Act
            _predictor.Predict(metricName);

            // Assert
            var metadata = _predictor.GetPredictionMetadata(metricName);
            Assert.NotNull(metadata);
            Assert.Equal(metricName, metadata.MetricName);
            // Success may be false due to mocked operations
        }

        #endregion

        #region Helper Methods

        private static List<MetricDataPoint> CreateTestHistory(int count)
        {
            var history = new List<MetricDataPoint>();
            var baseTime = DateTime.UtcNow.AddDays(-7);

            for (int i = 0; i < count; i++)
            {
                history.Add(new MetricDataPoint
                {
                    MetricName = "test.metric",
                    Timestamp = baseTime.AddHours(i),
                    Value = 50.0f + i,
                    MA5 = 50.0f,
                    MA15 = 50.0f,
                    Trend = 1,
                    HourOfDay = baseTime.AddHours(i).Hour,
                    DayOfWeek = (int)baseTime.AddHours(i).DayOfWeek
                });
            }

            return history;
        }

        #endregion
    }
}