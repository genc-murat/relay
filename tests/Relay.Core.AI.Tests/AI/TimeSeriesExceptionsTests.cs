using System;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI;

public class TimeSeriesExceptionsTests
{
    [Fact]
    public void TimeSeriesException_Should_Initialize_With_Message()
    {
        // Arrange
        var message = "Test time series error";

        // Act
        var exception = new TimeSeriesException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void TimeSeriesException_Should_Initialize_With_Message_And_InnerException()
    {
        // Arrange
        var message = "Test time series error";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new TimeSeriesException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void InsufficientDataException_Should_Initialize_With_Message()
    {
        // Arrange
        var message = "Not enough data for analysis";

        // Act
        var exception = new InsufficientDataException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
        Assert.IsAssignableFrom<TimeSeriesException>(exception);
    }

    [Fact]
    public void InsufficientDataException_Should_Initialize_With_Message_And_InnerException()
    {
        // Arrange
        var message = "Not enough data for analysis";
        var innerException = new ArgumentException("Invalid data");

        // Act
        var exception = new InsufficientDataException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.IsAssignableFrom<TimeSeriesException>(exception);
    }

    [Fact]
    public void ModelTrainingException_Should_Initialize_With_Message()
    {
        // Arrange
        var message = "Model training failed";

        // Act
        var exception = new ModelTrainingException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
        Assert.IsAssignableFrom<TimeSeriesException>(exception);
    }

    [Fact]
    public void ModelTrainingException_Should_Initialize_With_Message_And_InnerException()
    {
        // Arrange
        var message = "Model training failed";
        var innerException = new Exception("Training error");

        // Act
        var exception = new ModelTrainingException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.IsAssignableFrom<TimeSeriesException>(exception);
    }

    [Fact]
    public void ModelTrainingException_Should_Allow_Setting_ForecastingMethod()
    {
        // Arrange
        var message = "Model training failed";
        var forecastingMethod = ForecastingMethod.SSA;

        // Act
        var exception = new ModelTrainingException(message)
        {
            ForecastingMethod = forecastingMethod
        };

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(forecastingMethod, exception.ForecastingMethod);
    }

    [Fact]
    public void ModelTrainingException_Should_Have_Null_ForecastingMethod_By_Default()
    {
        // Arrange
        var message = "Model training failed";

        // Act
        var exception = new ModelTrainingException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.ForecastingMethod);
    }

    [Fact]
    public void ForecastingException_Should_Initialize_With_Message()
    {
        // Arrange
        var message = "Forecasting operation failed";

        // Act
        var exception = new ForecastingException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
        Assert.IsAssignableFrom<TimeSeriesException>(exception);
    }

    [Fact]
    public void ForecastingException_Should_Initialize_With_Message_And_InnerException()
    {
        // Arrange
        var message = "Forecasting operation failed";
        var innerException = new Exception("Forecast error");

        // Act
        var exception = new ForecastingException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.IsAssignableFrom<TimeSeriesException>(exception);
    }

    [Fact]
    public void AnomalyDetectionException_Should_Initialize_With_Message()
    {
        // Arrange
        var message = "Anomaly detection failed";

        // Act
        var exception = new AnomalyDetectionException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
        Assert.IsAssignableFrom<TimeSeriesException>(exception);
    }

    [Fact]
    public void AnomalyDetectionException_Should_Initialize_With_Message_And_InnerException()
    {
        // Arrange
        var message = "Anomaly detection failed";
        var innerException = new Exception("Detection error");

        // Act
        var exception = new AnomalyDetectionException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.IsAssignableFrom<TimeSeriesException>(exception);
    }

    [Fact]
    public void Exceptions_Should_Inherit_From_TimeSeriesException()
    {
        // Act
        var insufficientDataEx = new InsufficientDataException("test");
        var modelTrainingEx = new ModelTrainingException("test");
        var forecastingEx = new ForecastingException("test");
        var anomalyDetectionEx = new AnomalyDetectionException("test");

        // Assert
        Assert.IsAssignableFrom<TimeSeriesException>(insufficientDataEx);
        Assert.IsAssignableFrom<TimeSeriesException>(modelTrainingEx);
        Assert.IsAssignableFrom<TimeSeriesException>(forecastingEx);
        Assert.IsAssignableFrom<TimeSeriesException>(anomalyDetectionEx);
    }

    [Fact]
    public void Exceptions_Should_Be_Serializable()
    {
        // Arrange
        var message = "Test exception";
        var innerException = new Exception("Inner");

        // Act
        var exceptions = new Exception[]
        {
            new TimeSeriesException(message),
            new TimeSeriesException(message, innerException),
            new InsufficientDataException(message),
            new InsufficientDataException(message, innerException),
            new ModelTrainingException(message),
            new ModelTrainingException(message, innerException),
            new ForecastingException(message),
            new ForecastingException(message, innerException),
            new AnomalyDetectionException(message),
            new AnomalyDetectionException(message, innerException)
        };

        // Assert - All should be created without issues
        Assert.All(exceptions, ex => Assert.NotNull(ex));
        Assert.All(exceptions, ex => Assert.Equal(message, ex.Message));
    }
}