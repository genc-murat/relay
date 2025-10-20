using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Events;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaExecutionResultTests
{
    [Fact]
    public void SagaExecutionResult_WithSuccessfulExecution_ShouldInitialize()
    {
        // Arrange
        var data = new TestSagaData { Value = 100 };

        // Act
        var result = new SagaExecutionResult<TestSagaData>
        {
            Data = data,
            IsSuccess = true
        };

        // Assert
        Assert.Equal(data, result.Data);
        Assert.True(result.IsSuccess);
        Assert.Null(result.FailedStep);
        Assert.Null(result.Exception);
        Assert.False(result.CompensationSucceeded);
    }

    [Fact]
    public void SagaExecutionResult_WithFailedExecution_ShouldInitialize()
    {
        // Arrange
        var data = new TestSagaData { Value = 100 };
        var exception = new InvalidOperationException("Test error");

        // Act
        var result = new SagaExecutionResult<TestSagaData>
        {
            Data = data,
            IsSuccess = false,
            FailedStep = "ProcessPayment",
            Exception = exception,
            CompensationSucceeded = true
        };

        // Assert
        Assert.Equal(data, result.Data);
        Assert.False(result.IsSuccess);
        Assert.Equal("ProcessPayment", result.FailedStep);
        Assert.Equal(exception, result.Exception);
        Assert.True(result.CompensationSucceeded);
    }

    [Fact]
    public void SagaExecutionResult_WithNullException_ShouldWork()
    {
        // Arrange
        var data = new TestSagaData();

        // Act
        var result = new SagaExecutionResult<TestSagaData>
        {
            Data = data,
            IsSuccess = false,
            Exception = null
        };

        // Assert
        Assert.Null(result.Exception);
    }

    [Fact]
    public void SagaExecutionResult_WithNullFailedStep_ShouldWork()
    {
        // Arrange
        var data = new TestSagaData();

        // Act
        var result = new SagaExecutionResult<TestSagaData>
        {
            Data = data,
            IsSuccess = false,
            FailedStep = null
        };

        // Assert
        Assert.Null(result.FailedStep);
    }

    [Fact]
    public void SagaExecutionResult_DefaultValues_ShouldWork()
    {
        // Act
        var result = new SagaExecutionResult<TestSagaData>
        {
            Data = new TestSagaData()
        };

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.FailedStep);
        Assert.Null(result.Exception);
        Assert.False(result.CompensationSucceeded);
    }
}

public class SagaOptionsTests
{
    [Fact]
    public void SagaOptions_DefaultValues_ShouldBeSet()
    {
        // Act
        var options = new SagaOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(30), options.DefaultTimeout);
        Assert.True(options.AutoPersist);
        Assert.Equal(TimeSpan.FromSeconds(5), options.PersistenceInterval);
        Assert.False(options.AutoRetryFailedSteps);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(5), options.RetryDelay);
        Assert.True(options.UseExponentialBackoff);
        Assert.True(options.AutoCompensateOnFailure);
        Assert.True(options.ContinueCompensationOnError);
        Assert.Null(options.StepTimeout);
        Assert.Null(options.CompensationTimeout);
        Assert.True(options.TrackMetrics);
        Assert.True(options.EnableTelemetry);
        Assert.Null(options.OnSagaCompleted);
        Assert.Null(options.OnSagaFailed);
        Assert.Null(options.OnSagaCompensated);
    }

    [Fact]
    public void SagaOptions_CanSetEnabled()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.Enabled = false;

        // Assert
        Assert.False(options.Enabled);
    }

    [Fact]
    public void SagaOptions_CanSetMaxRetryAttempts()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.MaxRetryAttempts = 5;

        // Assert
        Assert.Equal(5, options.MaxRetryAttempts);
    }

    [Fact]
    public void SagaOptions_CanSetRetryDelay()
    {
        // Arrange
        var options = new SagaOptions();
        var delay = TimeSpan.FromSeconds(10);

        // Act
        options.RetryDelay = delay;

        // Assert
        Assert.Equal(delay, options.RetryDelay);
    }

    [Fact]
    public void SagaOptions_CanSetAutoPersist()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.AutoPersist = false;

        // Assert
        Assert.False(options.AutoPersist);
    }

    [Fact]
    public void SagaOptions_CanSetCompensationTimeout()
    {
        // Arrange
        var options = new SagaOptions();
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        options.CompensationTimeout = timeout;

        // Assert
        Assert.Equal(timeout, options.CompensationTimeout);
    }

    [Fact]
    public void SagaOptions_CanSetTrackMetrics()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.TrackMetrics = false;

        // Assert
        Assert.False(options.TrackMetrics);
    }

    [Fact]
    public void SagaOptions_CanSetEnableTelemetry()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.EnableTelemetry = false;

        // Assert
        Assert.False(options.EnableTelemetry);
    }

    [Fact]
    public void SagaOptions_CanSetOnSagaCompleted()
    {
        // Arrange
        var options = new SagaOptions();
        var called = false;
        Action<SagaCompletedEventArgs> handler = args => called = true;

        // Act
        options.OnSagaCompleted = handler;

        // Assert
        Assert.NotNull(options.OnSagaCompleted);
        options.OnSagaCompleted!(new SagaCompletedEventArgs());
        Assert.True(called);
    }

    [Fact]
    public void SagaOptions_CanSetOnSagaFailed()
    {
        // Arrange
        var options = new SagaOptions();
        var called = false;
        Action<SagaFailedEventArgs> handler = args => called = true;

        // Act
        options.OnSagaFailed = handler;

        // Assert
        Assert.NotNull(options.OnSagaFailed);
        options.OnSagaFailed!(new SagaFailedEventArgs());
        Assert.True(called);
    }

    [Fact]
    public void SagaOptions_CanSetOnSagaCompensated()
    {
        // Arrange
        var options = new SagaOptions();
        var called = false;
        Action<SagaCompensatedEventArgs> handler = args => called = true;

        // Act
        options.OnSagaCompensated = handler;

        // Assert
        Assert.NotNull(options.OnSagaCompensated);
        options.OnSagaCompensated!(new SagaCompensatedEventArgs());
        Assert.True(called);
    }

    [Fact]
    public void SagaOptions_WithZeroRetries_ShouldBeAllowed()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.MaxRetryAttempts = 0;

        // Assert
        Assert.Equal(0, options.MaxRetryAttempts);
    }

    [Fact]
    public void SagaOptions_WithVeryLargeRetryDelay_ShouldBeAllowed()
    {
        // Arrange
        var options = new SagaOptions();
        var longDelay = TimeSpan.FromMinutes(30);

        // Act
        options.RetryDelay = longDelay;

        // Assert
        Assert.Equal(longDelay, options.RetryDelay);
    }

    [Fact]
    public void SagaOptions_WithAllCallbacksSet_ShouldWork()
    {
        // Arrange
        var options = new SagaOptions();
        var completedCalled = false;
        var failedCalled = false;
        var compensatedCalled = false;

        // Act
        options.OnSagaCompleted = args => completedCalled = true;
        options.OnSagaFailed = args => failedCalled = true;
        options.OnSagaCompensated = args => compensatedCalled = true;

        // Assert
        Assert.NotNull(options.OnSagaCompleted);
        Assert.NotNull(options.OnSagaFailed);
        Assert.NotNull(options.OnSagaCompensated);

        options.OnSagaCompleted!(new SagaCompletedEventArgs());
        options.OnSagaFailed!(new SagaFailedEventArgs());
        options.OnSagaCompensated!(new SagaCompensatedEventArgs());

        Assert.True(completedCalled);
        Assert.True(failedCalled);
        Assert.True(compensatedCalled);
    }

    [Fact]
    public void SagaOptions_CanSetAutoRetryFailedSteps()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.AutoRetryFailedSteps = true;

        // Assert
        Assert.True(options.AutoRetryFailedSteps);
    }

    [Fact]
    public void SagaOptions_CanSetUseExponentialBackoff()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.UseExponentialBackoff = false;

        // Assert
        Assert.False(options.UseExponentialBackoff);
    }

    [Fact]
    public void SagaOptions_CanSetAutoCompensateOnFailure()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.AutoCompensateOnFailure = false;

        // Assert
        Assert.False(options.AutoCompensateOnFailure);
    }

    [Fact]
    public void SagaOptions_CanSetStepTimeout()
    {
        // Arrange
        var options = new SagaOptions();
        var timeout = TimeSpan.FromSeconds(45);

        // Act
        options.StepTimeout = timeout;

        // Assert
        Assert.Equal(timeout, options.StepTimeout);
    }

    [Fact]
    public void SagaOptions_CanSetPersistenceInterval()
    {
        // Arrange
        var options = new SagaOptions();
        var interval = TimeSpan.FromSeconds(10);

        // Act
        options.PersistenceInterval = interval;

        // Assert
        Assert.Equal(interval, options.PersistenceInterval);
    }
}
