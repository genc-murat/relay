using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Moq;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MetricDataPoint = Relay.Core.AI.MetricDataPoint;

namespace Relay.Core.Tests.AI.Analysis.TimeSeries.Predictors;

public class ForecastingTrainerTests
{
    private readonly Mock<ILogger<ForecastingTrainer>> _loggerMock;
    private readonly Mock<ITimeSeriesRepository> _repositoryMock;
    private readonly Mock<IForecastingModelManager> _modelManagerMock;
    private readonly Mock<IForecastingMethodManager> _methodManagerMock;
    private readonly ForecastingConfiguration _config;
    private readonly ForecastingTrainer _trainer;

    public ForecastingTrainerTests()
    {
        _loggerMock = new Mock<ILogger<ForecastingTrainer>>();
        _repositoryMock = new Mock<ITimeSeriesRepository>();
        _modelManagerMock = new Mock<IForecastingModelManager>();
        _methodManagerMock = new Mock<IForecastingMethodManager>();
        _config = new ForecastingConfiguration
        {
            AutoTrainOnForecast = true,
            MinimumDataPoints = 10,
            TrainingDataWindowDays = 7,
            DefaultForecastHorizon = 12,
            MlContextSeed = 42
        };

        _trainer = new ForecastingTrainer(
            _loggerMock.Object,
            _repositoryMock.Object,
            _modelManagerMock.Object,
            _methodManagerMock.Object,
            _config);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingTrainer(null!, _repositoryMock.Object, _modelManagerMock.Object, _methodManagerMock.Object, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Repository_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingTrainer(_loggerMock.Object, null!, _modelManagerMock.Object, _methodManagerMock.Object, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_ModelManager_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingTrainer(_loggerMock.Object, _repositoryMock.Object, null!, _methodManagerMock.Object, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_MethodManager_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingTrainer(_loggerMock.Object, _repositoryMock.Object, _modelManagerMock.Object, null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingTrainer(_loggerMock.Object, _repositoryMock.Object, _modelManagerMock.Object, _methodManagerMock.Object, null!));
    }

    #endregion

    #region TrainModel Tests

    [Fact]
    public void TrainModel_Should_Throw_When_MetricName_Is_Null()
    {
        var exception = Assert.Throws<ArgumentException>(() => _trainer.TrainModel(null!));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void TrainModel_Should_Throw_When_MetricName_Is_Empty()
    {
        var exception = Assert.Throws<ArgumentException>(() => _trainer.TrainModel(string.Empty));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void TrainModel_Should_Throw_When_MetricName_Is_Whitespace()
    {
        var exception = Assert.Throws<ArgumentException>(() => _trainer.TrainModel("   "));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void TrainModel_Should_Throw_InsufficientDataException_When_Data_Is_Insufficient()
    {
        // Arrange
        var metricName = "test.metric";
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(ForecastingMethod.SSA);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(5)); // Less than minimum 10

        // Act & Assert
        var exception = Assert.Throws<InsufficientDataException>(() => _trainer.TrainModel(metricName));
        Assert.Contains("Insufficient data", exception.Message);
        Assert.Equal(metricName, exception.MetricName);
        Assert.Equal("TrainSSAModel", exception.Operation);
        Assert.Equal(10, exception.MinimumRequired);
        Assert.Equal(5, exception.ActualCount);
    }

    [Fact]
    public void TrainModel_Should_Use_Specified_Method_When_Provided()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.MovingAverage;
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var model = Mock.Of<ITransformer>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act
        _trainer.TrainModel(metricName, method);

        // Assert
        _methodManagerMock.Verify(m => m.GetForecastingMethod(metricName), Times.Never); // Should not call GetForecastingMethod when method is specified
        _methodManagerMock.Verify(m => m.GetStrategy(method), Times.Once);
        strategyMock.Verify(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), _config.DefaultForecastHorizon), Times.Once);
        _modelManagerMock.Verify(m => m.StoreModel(metricName, model, method), Times.Once);
    }

    [Fact]
    public void TrainModel_Should_Get_Method_From_MethodManager_When_Not_Specified()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var model = Mock.Of<ITransformer>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act
        _trainer.TrainModel(metricName);

        // Assert
        _methodManagerMock.Verify(m => m.GetForecastingMethod(metricName), Times.Once);
        _methodManagerMock.Verify(m => m.GetStrategy(method), Times.Once);
        strategyMock.Verify(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), _config.DefaultForecastHorizon), Times.Once);
        _modelManagerMock.Verify(m => m.StoreModel(metricName, model, method), Times.Once);
    }

    [Fact]
    public void TrainModel_Should_Log_Warning_When_Strategy_Is_Null()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns((IForecastingStrategy?)null);

        // Act
        _trainer.TrainModel(metricName);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Unsupported forecasting method")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _modelManagerMock.Verify(m => m.StoreModel(It.IsAny<string>(), It.IsAny<ITransformer>(), It.IsAny<ForecastingMethod>()), Times.Never);
    }

    [Fact]
    public void TrainModel_Should_Log_Information_On_Successful_Training()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var model = Mock.Of<ITransformer>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act
        _trainer.TrainModel(metricName);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Training SSA forecast model") && o.ToString()!.Contains("15 data points")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SSA forecast model trained") && o.ToString()!.Contains("horizon=12")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void TrainModel_Should_Throw_ModelTrainingException_With_ForecastingMethod_When_Training_Fails()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var trainingException = new Exception("Training failed");
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>()))
            .Throws(trainingException);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act & Assert
        var exception = Assert.Throws<ModelTrainingException>(() => _trainer.TrainModel(metricName));
        Assert.Equal(method, exception.ForecastingMethod);
        Assert.Contains($"Failed to train {method} model for {metricName}", exception.Message);
        Assert.Equal(trainingException, exception.InnerException);

        // Verify logging still happens
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error training SSA forecast model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _modelManagerMock.Verify(m => m.StoreModel(It.IsAny<string>(), It.IsAny<ITransformer>(), It.IsAny<ForecastingMethod>()), Times.Never);
    }

    [Fact]
    public void TrainModel_Should_Throw_ModelTrainingException_With_Correct_ForecastingMethod_For_Different_Methods()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.ExponentialSmoothing;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var trainingException = new InvalidOperationException("Exponential smoothing training failed");
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>()))
            .Throws(trainingException);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act & Assert
        var exception = Assert.Throws<ModelTrainingException>(() => _trainer.TrainModel(metricName));
        Assert.Equal(method, exception.ForecastingMethod);
        Assert.Contains($"Failed to train {method} model for {metricName}", exception.Message);
        Assert.Equal(trainingException, exception.InnerException);
    }

    [Fact]
    public void TrainModel_Should_Rethrow_InsufficientDataException()
    {
        // Arrange
        var metricName = "test.metric";
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(ForecastingMethod.SSA);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(5));

        // Act & Assert
        Assert.Throws<InsufficientDataException>(() => _trainer.TrainModel(metricName));
    }

    #endregion

    #region TrainModelAsync Tests

    [Fact]
    public async Task TrainModelAsync_Should_Call_Sync_Method()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var model = Mock.Of<ITransformer>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act
        await _trainer.TrainModelAsync(metricName);

        // Assert
        _methodManagerMock.Verify(m => m.GetStrategy(method), Times.Once);
        strategyMock.Verify(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), _config.DefaultForecastHorizon), Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_Should_Respect_CancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _trainer.TrainModelAsync("test.metric", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task TrainModelAsync_Should_Throw_CancellationException_When_Cancelled_During_Execution()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);

        // Set up repository to return data, but strategy will be slow (simulated by throwing OperationCanceledException)
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>()))
            .Throws(new OperationCanceledException());

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _trainer.TrainModelAsync(metricName, cancellationToken: cts.Token));
    }

    #endregion

    #region HasSufficientData Tests

    [Fact]
    public void HasSufficientData_Should_Throw_When_MetricName_Is_Invalid()
    {
        var exception = Assert.Throws<ArgumentException>(() => _trainer.HasSufficientData(null!, out _));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void HasSufficientData_Should_Return_True_When_Data_Is_Sufficient()
    {
        // Arrange
        var metricName = "test.metric";
        var history = CreateTestHistory(15);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(history);

        // Act
        var result = _trainer.HasSufficientData(metricName, out var actualCount);

        // Assert
        Assert.True(result);
        Assert.Equal(15, actualCount);
    }

    [Fact]
    public void HasSufficientData_Should_Return_False_When_Data_Is_Insufficient()
    {
        // Arrange
        var metricName = "test.metric";
        var history = CreateTestHistory(5);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(history);

        // Act
        var result = _trainer.HasSufficientData(metricName, out var actualCount);

        // Assert
        Assert.False(result);
        Assert.Equal(5, actualCount);
    }

    [Fact]
    public void HasSufficientData_Should_Return_False_When_No_Data()
    {
        // Arrange
        var metricName = "test.metric";
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(new List<MetricDataPoint>());

        // Act
        var result = _trainer.HasSufficientData(metricName, out var actualCount);

        // Assert
        Assert.False(result);
        Assert.Equal(0, actualCount);
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