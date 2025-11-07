using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Moq;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Collections.Generic;
using System.Linq;
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

    [Fact]
    public void TrainModel_Should_Use_Configured_DefaultForecastHorizon()
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
        strategyMock.Verify(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), _config.DefaultForecastHorizon), Times.Once);
    }

    [Fact]
    public void TrainModel_Should_Respect_TrainingDataWindowDays_Configuration()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);

        var expectedTimeSpan = TimeSpan.FromDays(_config.TrainingDataWindowDays);
        _repositoryMock.Setup(r => r.GetHistory(metricName, expectedTimeSpan)).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var model = Mock.Of<ITransformer>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act
        _trainer.TrainModel(metricName);

        // Assert - Repository is called twice: once in HasSufficientData, once in TrainModel
        _repositoryMock.Verify(r => r.GetHistory(metricName, expectedTimeSpan), Times.Exactly(2));
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

    [Fact]
    public void TrainModel_Should_Throw_ModelTrainingException_When_Repository_Throws_In_Training()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);

        // Setup repository to throw during the second call (in TrainModel, not in HasSufficientData)
        var callCount = 0;
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    return CreateTestHistory(15); // First call for HasSufficientData
                else
                    throw new Exception("Database connection failed"); // Second call in TrainModel
            });

        var strategyMock = new Mock<IForecastingStrategy>();
        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act & Assert
        var exception = Assert.Throws<ModelTrainingException>(() => _trainer.TrainModel(metricName));
        Assert.Equal(method, exception.ForecastingMethod);
        Assert.Contains($"Failed to train {method} model for {metricName}", exception.Message);
        Assert.Contains("Database connection failed", exception.InnerException?.Message);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error training SSA forecast model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void TrainModel_Should_Throw_Exception_When_MethodManager_Throws()
    {
        // Arrange
        var metricName = "test.metric";
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName))
            .Throws(new Exception("Method manager error"));

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => _trainer.TrainModel(metricName));
        Assert.Equal("Method manager error", exception.Message);
    }

    [Fact]
    public void TrainModel_Should_Throw_ModelTrainingException_When_ModelManager_Throws()
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

        _modelManagerMock.Setup(m => m.StoreModel(metricName, model, method))
            .Throws(new Exception("Storage failed"));

        // Act & Assert
        var exception = Assert.Throws<ModelTrainingException>(() => _trainer.TrainModel(metricName));
        Assert.Equal(method, exception.ForecastingMethod);
        Assert.Contains($"Failed to train {method} model for {metricName}", exception.Message);
        Assert.Contains("Storage failed", exception.InnerException?.Message);
    }

    [Fact]
    public void TrainModel_Should_Succeed_When_Data_Equals_Minimum_Threshold()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);

        // Create exactly the minimum number of data points
        var history = CreateTestHistory(_config.MinimumDataPoints);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(history);

        var strategyMock = new Mock<IForecastingStrategy>();
        var model = Mock.Of<ITransformer>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act
        _trainer.TrainModel(metricName);

        // Assert - Should not throw InsufficientDataException
        _methodManagerMock.Verify(m => m.GetStrategy(method), Times.Once);
        _modelManagerMock.Verify(m => m.StoreModel(metricName, model, method), Times.Once);
    }

    [Theory]
    [InlineData(ForecastingMethod.SSA)]
    [InlineData(ForecastingMethod.ExponentialSmoothing)]
    [InlineData(ForecastingMethod.MovingAverage)]
    public void TrainModel_Should_Succeed_With_All_Forecasting_Methods(ForecastingMethod method)
    {
        // Arrange
        var metricName = "test.metric";
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var model = Mock.Of<ITransformer>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act
        _trainer.TrainModel(metricName);

        // Assert
        _methodManagerMock.Verify(m => m.GetStrategy(method), Times.Once);
        _modelManagerMock.Verify(m => m.StoreModel(metricName, model, method), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"{method} forecast model trained")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("métric_with_accents")]
    [InlineData("指标")]  // Chinese characters
    [InlineData("metric@domain.com")]
    [InlineData("metric#tag")]
    [InlineData("metric with spaces")]
    public void TrainModel_Should_Accept_Various_Metric_Name_Formats(string metricName)
    {
        // Arrange
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
        _modelManagerMock.Verify(m => m.StoreModel(metricName, model, method), Times.Once);
    }

    [Fact]
    public void TrainModel_Should_Rethrow_OperationCanceledException()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>()))
            .Throws(new OperationCanceledException("Training was cancelled"));

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() => _trainer.TrainModel(metricName));

        // Verify it was re-thrown, not wrapped in ModelTrainingException
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never); // Should not log as error since it's re-thrown
    }

    [Fact]
    public void TrainModel_Should_Handle_Different_Exception_Types()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var customException = new InvalidOperationException("Custom training error");
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>()))
            .Throws(customException);

        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act & Assert
        var exception = Assert.Throws<ModelTrainingException>(() => _trainer.TrainModel(metricName));
        Assert.Equal(method, exception.ForecastingMethod);
        Assert.Equal(customException, exception.InnerException);
        Assert.Contains("Custom training error", exception.Message);
    }

    [Fact]
    public void TrainModel_Should_Preserve_Exception_Properties_In_InsufficientDataException()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.ExponentialSmoothing;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(3)); // Less than minimum

        // Act & Assert
        var exception = Assert.Throws<InsufficientDataException>(() => _trainer.TrainModel(metricName));
        Assert.Equal(metricName, exception.MetricName);
        Assert.Equal($"Train{method}Model", exception.Operation);
        Assert.Equal(_config.MinimumDataPoints, exception.MinimumRequired);
        Assert.Equal(3, exception.ActualCount);
        Assert.Contains($"Insufficient data for {method} forecasting", exception.Message);
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

    [Fact]
    public async Task TrainModelAsync_Should_Propagate_Repository_Exceptions()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(method);

        var callCount = 0;
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    return CreateTestHistory(15);
                else
                    throw new Exception("Async repository error");
            });

        var strategyMock = new Mock<IForecastingStrategy>();
        _methodManagerMock.Setup(m => m.GetStrategy(method)).Returns(strategyMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ModelTrainingException>(() =>
            _trainer.TrainModelAsync(metricName));
        Assert.Contains("Async repository error", exception.InnerException?.Message);
    }

    [Fact]
    public async Task TrainModelAsync_Should_Propagate_MethodManager_Exceptions()
    {
        // Arrange
        var metricName = "test.metric";
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName))
            .Throws(new Exception("Async method manager error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _trainer.TrainModelAsync(metricName));
        Assert.Equal("Async method manager error", exception.Message);
    }

    [Fact]
    public async Task TrainModelAsync_Should_Propagate_ModelManager_Exceptions()
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

        _modelManagerMock.Setup(m => m.StoreModel(metricName, model, method))
            .Throws(new Exception("Async storage error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ModelTrainingException>(() =>
            _trainer.TrainModelAsync(metricName));
        Assert.Contains("Async storage error", exception.InnerException?.Message);
    }

    [Fact]
    public async Task TrainModelAsync_Should_Handle_Concurrent_Training_Operations()
    {
        // Arrange
        var metrics = new[] { "metric1", "metric2", "metric3" };
        var exceptions = new List<Exception>();

        foreach (var metric in metrics)
        {
            _methodManagerMock.Setup(m => m.GetForecastingMethod(metric)).Returns(ForecastingMethod.SSA);
            _repositoryMock.Setup(r => r.GetHistory(metric, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

            var strategyMock = new Mock<IForecastingStrategy>();
            var model = Mock.Of<ITransformer>();
            strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

            _methodManagerMock.Setup(m => m.GetStrategy(ForecastingMethod.SSA)).Returns(strategyMock.Object);
        }

        // Act
        var tasks = metrics.Select(async metric =>
        {
            try
            {
                await _trainer.TrainModelAsync(metric);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Trainer_Should_Be_Thread_Safe_For_Concurrent_Training()
    {
        // Arrange
        var metrics = Enumerable.Range(0, 20).Select(i => $"metric{i}").ToList();
        var exceptions = new List<Exception>();

        // Setup mocks for all metrics
        foreach (var metric in metrics)
        {
            _methodManagerMock.Setup(m => m.GetForecastingMethod(metric)).Returns(ForecastingMethod.SSA);
            _repositoryMock.Setup(r => r.GetHistory(metric, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

            var strategyMock = new Mock<IForecastingStrategy>();
            var model = Mock.Of<ITransformer>();
            strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

            _methodManagerMock.Setup(m => m.GetStrategy(ForecastingMethod.SSA)).Returns(strategyMock.Object);
        }

        // Act
        var tasks = metrics.Select(async metric =>
        {
            try
            {
                // Perform multiple training operations concurrently
                await Task.WhenAll(
                    Task.Run(() => _trainer.TrainModel(metric)),
                    Task.Run(() => _trainer.TrainModel(metric)),
                    Task.Run(() => _trainer.TrainModel(metric))
                );
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public void Trainer_Should_Handle_Concurrent_Training_Of_Same_Metric()
    {
        // Arrange
        var metricName = "concurrent.metric";
        var taskCount = 10;
        var exceptions = new List<Exception>();

        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(ForecastingMethod.SSA);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(CreateTestHistory(15));

        var strategyMock = new Mock<IForecastingStrategy>();
        var model = Mock.Of<ITransformer>();
        strategyMock.Setup(s => s.TrainModel(It.IsAny<MLContext>(), It.IsAny<List<MetricDataPoint>>(), It.IsAny<int>())).Returns(model);

        _methodManagerMock.Setup(m => m.GetStrategy(ForecastingMethod.SSA)).Returns(strategyMock.Object);

        // Act
        var tasks = Enumerable.Range(0, taskCount).Select(async i =>
        {
            try
            {
                await Task.Run(() => _trainer.TrainModel(metricName));
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Empty(exceptions);
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

    [Fact]
    public void HasSufficientData_Should_Throw_When_Repository_Throws()
    {
        // Arrange
        var metricName = "test.metric";
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>()))
            .Throws(new Exception("Repository error"));

        // Act & Assert
        Assert.Throws<Exception>(() => _trainer.HasSufficientData(metricName, out _));
    }

    [Fact]
    public void HasSufficientData_Should_Handle_Data_Exactly_At_Minimum()
    {
        // Arrange
        var metricName = "test.metric";
        var history = CreateTestHistory(_config.MinimumDataPoints);
        _repositoryMock.Setup(r => r.GetHistory(metricName, It.IsAny<TimeSpan>())).Returns(history);

        // Act
        var result = _trainer.HasSufficientData(metricName, out var actualCount);

        // Assert
        Assert.True(result);
        Assert.Equal(_config.MinimumDataPoints, actualCount);
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