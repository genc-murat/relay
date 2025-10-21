using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.TimeSeries;

public class ForecastingServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ForecastingService _service;
    private readonly Mock<ITimeSeriesRepository> _repositoryMock;
    private readonly Mock<IForecastingTrainer> _trainerMock;
    private readonly Mock<IForecastingPredictor> _predictorMock;
    private readonly Mock<IForecastingMethodManager> _methodManagerMock;

    public ForecastingServiceTests()
    {
        _repositoryMock = new Mock<ITimeSeriesRepository>();
        _trainerMock = new Mock<IForecastingTrainer>();
        _predictorMock = new Mock<IForecastingPredictor>();
        _methodManagerMock = new Mock<IForecastingMethodManager>();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(_repositoryMock.Object);
        services.AddSingleton(_trainerMock.Object);
        services.AddSingleton(_predictorMock.Object);
        services.AddSingleton(_methodManagerMock.Object);
        services.AddTransient<ForecastingService>();

        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<ForecastingService>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_trainerMock.Object);
        services.AddSingleton(_predictorMock.Object);
        services.AddSingleton(_methodManagerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingService(null!, _trainerMock.Object, _predictorMock.Object, _methodManagerMock.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Trainer_Is_Null()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ForecastingService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingService(logger, null!, _predictorMock.Object, _methodManagerMock.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Predictor_Is_Null()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ForecastingService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingService(logger, _trainerMock.Object, null!, _methodManagerMock.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_When_MethodManager_Is_Null()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ForecastingService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingService(logger, _trainerMock.Object, _predictorMock.Object, null!));
    }

    #endregion

    #region TrainForecastModel Tests

    [Fact]
    public void TrainForecastModel_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.TrainForecastModel(null!));
    }

    [Fact]
    public void TrainForecastModel_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.TrainForecastModel("   "));
    }

    [Fact]
    public void TrainForecastModel_Should_Call_Trainer_With_Correct_Parameters()
    {
        // Arrange
        var metricName = "cpu_usage";
        var method = ForecastingMethod.SSA;

        // Act
        _service.TrainForecastModel(metricName, method);

        // Assert
        _trainerMock.Verify(t => t.TrainModel(metricName, method), Times.Once);
    }

    [Fact]
    public void TrainForecastModel_Should_Set_Method_When_Method_Is_Provided()
    {
        // Arrange
        var metricName = "cpu_usage";
        var method = ForecastingMethod.SSA;

        // Act
        _service.TrainForecastModel(metricName, method);

        // Assert
        _methodManagerMock.Verify(m => m.SetForecastingMethod(metricName, method), Times.Once);
    }

    [Fact]
    public void TrainForecastModel_Should_Not_Set_Method_When_Method_Is_Not_Provided()
    {
        // Arrange
        var metricName = "cpu_usage";

        // Act
        _service.TrainForecastModel(metricName);

        // Assert
        _methodManagerMock.Verify(m => m.SetForecastingMethod(It.IsAny<string>(), It.IsAny<ForecastingMethod>()), Times.Never);
    }

    [Fact]
    public void TrainForecastModel_Should_Call_Trainer_With_Null_Method_When_Not_Specified()
    {
        // Arrange
        var metricName = "memory_usage";

        // Act
        _service.TrainForecastModel(metricName);

        // Assert
        _trainerMock.Verify(t => t.TrainModel(metricName, null), Times.Once);
    }

    #endregion

    #region TrainForecastModelAsync Tests

    [Fact]
    public async Task TrainForecastModelAsync_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.TrainForecastModelAsync(null!));
    }

    [Fact]
    public async Task TrainForecastModelAsync_Should_Call_TrainerAsync_With_Correct_Parameters()
    {
        // Arrange
        var metricName = "cpu_usage";
        var method = ForecastingMethod.ExponentialSmoothing;
        var cancellationToken = new CancellationToken();

        // Act
        await _service.TrainForecastModelAsync(metricName, method, cancellationToken);

        // Assert
        _trainerMock.Verify(t => t.TrainModelAsync(metricName, method, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task TrainForecastModelAsync_Should_Set_Method_When_Method_Is_Provided()
    {
        // Arrange
        var metricName = "cpu_usage";
        var method = ForecastingMethod.ExponentialSmoothing;
        var cancellationToken = new CancellationToken();

        // Act
        await _service.TrainForecastModelAsync(metricName, method, cancellationToken);

        // Assert
        _methodManagerMock.Verify(m => m.SetForecastingMethod(metricName, method), Times.Once);
    }

    [Fact]
    public async Task TrainForecastModelAsync_Should_Not_Set_Method_When_Method_Is_Not_Provided()
    {
        // Arrange
        var metricName = "cpu_usage";
        var cancellationToken = new CancellationToken();

        // Act
        await _service.TrainForecastModelAsync(metricName, cancellationToken: cancellationToken);

        // Assert
        _methodManagerMock.Verify(m => m.SetForecastingMethod(It.IsAny<string>(), It.IsAny<ForecastingMethod>()), Times.Never);
    }

    #endregion

    #region Forecast Tests

    [Fact]
    public void Forecast_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.Forecast(null!));
    }

    [Fact]
    public void Forecast_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.Forecast("   "));
    }

    [Fact]
    public void Forecast_Should_Throw_When_Horizon_Is_Zero()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Forecast("cpu", 0));
    }

    [Fact]
    public void Forecast_Should_Throw_When_Horizon_Is_Negative()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Forecast("cpu", -1));
    }

    [Fact]
    public void Forecast_Should_Call_Predictor_With_Correct_Parameters()
    {
        // Arrange
        var metricName = "cpu_usage";
        var horizon = 24;
        var expectedResult = CreateTestForecastResult();
        _predictorMock.Setup(p => p.Predict(metricName, horizon)).Returns(expectedResult);

        // Act
        var result = _service.Forecast(metricName, horizon);

        // Assert
        Assert.Equal(expectedResult, result);
        _predictorMock.Verify(p => p.Predict(metricName, horizon), Times.Once);
    }

    #endregion

    #region ForecastAsync Tests

    [Fact]
    public async Task ForecastAsync_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ForecastAsync(null!));
    }

    [Fact]
    public async Task ForecastAsync_Should_Call_PredictorAsync_With_Correct_Parameters()
    {
        // Arrange
        var metricName = "memory_usage";
        var horizon = 12;
        var cancellationToken = new CancellationToken();
        var expectedResult = CreateTestForecastResult();
        _predictorMock.Setup(p => p.PredictAsync(metricName, horizon, cancellationToken))
                     .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.ForecastAsync(metricName, horizon, cancellationToken);

        // Assert
        Assert.Equal(expectedResult, result);
        _predictorMock.Verify(p => p.PredictAsync(metricName, horizon, cancellationToken), Times.Once);
    }

    #endregion

    #region GetForecastingMethod Tests

    [Fact]
    public void GetForecastingMethod_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GetForecastingMethod(null!));
    }

    [Fact]
    public void GetForecastingMethod_Should_Call_MethodManager()
    {
        // Arrange
        var metricName = "cpu_usage";
        var expectedMethod = ForecastingMethod.SSA;
        _methodManagerMock.Setup(m => m.GetForecastingMethod(metricName)).Returns(expectedMethod);

        // Act
        var result = _service.GetForecastingMethod(metricName);

        // Assert
        Assert.Equal(expectedMethod, result);
        _methodManagerMock.Verify(m => m.GetForecastingMethod(metricName), Times.Once);
    }

    #endregion

    #region SetForecastingMethod Tests

    [Fact]
    public void SetForecastingMethod_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.SetForecastingMethod(null!, ForecastingMethod.SSA));
    }

    [Fact]
    public void SetForecastingMethod_Should_Call_MethodManager()
    {
        // Arrange
        var metricName = "cpu_usage";
        var method = ForecastingMethod.ExponentialSmoothing;

        // Act
        _service.SetForecastingMethod(metricName, method);

        // Assert
        _methodManagerMock.Verify(m => m.SetForecastingMethod(metricName, method), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static MetricForecastResult CreateTestForecastResult()
    {
        return new MetricForecastResult
        {
            ForecastedValues = new float[] { 1.0f, 2.0f, 3.0f },
            LowerBound = new float[] { 0.8f, 1.8f, 2.8f },
            UpperBound = new float[] { 1.2f, 2.2f, 3.2f }
        };
    }

    #endregion
}