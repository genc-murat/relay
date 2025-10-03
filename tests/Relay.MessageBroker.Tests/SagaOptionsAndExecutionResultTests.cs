using FluentAssertions;
using Relay.MessageBroker.Saga;
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
        result.Data.Should().Be(data);
        result.IsSuccess.Should().BeTrue();
        result.FailedStep.Should().BeNull();
        result.Exception.Should().BeNull();
        result.CompensationSucceeded.Should().BeFalse();
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
        result.Data.Should().Be(data);
        result.IsSuccess.Should().BeFalse();
        result.FailedStep.Should().Be("ProcessPayment");
        result.Exception.Should().Be(exception);
        result.CompensationSucceeded.Should().BeTrue();
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
        result.Exception.Should().BeNull();
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
        result.FailedStep.Should().BeNull();
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
        result.IsSuccess.Should().BeFalse();
        result.FailedStep.Should().BeNull();
        result.Exception.Should().BeNull();
        result.CompensationSucceeded.Should().BeFalse();
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
        options.Enabled.Should().BeTrue();
        options.DefaultTimeout.Should().Be(TimeSpan.FromMinutes(30));
        options.AutoPersist.Should().BeTrue();
        options.PersistenceInterval.Should().Be(TimeSpan.FromSeconds(5));
        options.AutoRetryFailedSteps.Should().BeFalse();
        options.MaxRetryAttempts.Should().Be(3);
        options.RetryDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.UseExponentialBackoff.Should().BeTrue();
        options.AutoCompensateOnFailure.Should().BeTrue();
        options.ContinueCompensationOnError.Should().BeTrue();
        options.StepTimeout.Should().BeNull();
        options.CompensationTimeout.Should().BeNull();
        options.TrackMetrics.Should().BeTrue();
        options.EnableTelemetry.Should().BeTrue();
        options.OnSagaCompleted.Should().BeNull();
        options.OnSagaFailed.Should().BeNull();
        options.OnSagaCompensated.Should().BeNull();
    }

    [Fact]
    public void SagaOptions_CanSetEnabled()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.Enabled = false;

        // Assert
        options.Enabled.Should().BeFalse();
    }

    [Fact]
    public void SagaOptions_CanSetMaxRetryAttempts()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.MaxRetryAttempts = 5;

        // Assert
        options.MaxRetryAttempts.Should().Be(5);
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
        options.RetryDelay.Should().Be(delay);
    }

    [Fact]
    public void SagaOptions_CanSetAutoPersist()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.AutoPersist = false;

        // Assert
        options.AutoPersist.Should().BeFalse();
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
        options.CompensationTimeout.Should().Be(timeout);
    }

    [Fact]
    public void SagaOptions_CanSetTrackMetrics()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.TrackMetrics = false;

        // Assert
        options.TrackMetrics.Should().BeFalse();
    }

    [Fact]
    public void SagaOptions_CanSetEnableTelemetry()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.EnableTelemetry = false;

        // Assert
        options.EnableTelemetry.Should().BeFalse();
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
        options.OnSagaCompleted.Should().NotBeNull();
        options.OnSagaCompleted!(new SagaCompletedEventArgs());
        called.Should().BeTrue();
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
        options.OnSagaFailed.Should().NotBeNull();
        options.OnSagaFailed!(new SagaFailedEventArgs());
        called.Should().BeTrue();
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
        options.OnSagaCompensated.Should().NotBeNull();
        options.OnSagaCompensated!(new SagaCompensatedEventArgs());
        called.Should().BeTrue();
    }

    [Fact]
    public void SagaOptions_WithZeroRetries_ShouldBeAllowed()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.MaxRetryAttempts = 0;

        // Assert
        options.MaxRetryAttempts.Should().Be(0);
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
        options.RetryDelay.Should().Be(longDelay);
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
        options.OnSagaCompleted.Should().NotBeNull();
        options.OnSagaFailed.Should().NotBeNull();
        options.OnSagaCompensated.Should().NotBeNull();

        options.OnSagaCompleted!(new SagaCompletedEventArgs());
        options.OnSagaFailed!(new SagaFailedEventArgs());
        options.OnSagaCompensated!(new SagaCompensatedEventArgs());

        completedCalled.Should().BeTrue();
        failedCalled.Should().BeTrue();
        compensatedCalled.Should().BeTrue();
    }

    [Fact]
    public void SagaOptions_CanSetAutoRetryFailedSteps()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.AutoRetryFailedSteps = true;

        // Assert
        options.AutoRetryFailedSteps.Should().BeTrue();
    }

    [Fact]
    public void SagaOptions_CanSetUseExponentialBackoff()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.UseExponentialBackoff = false;

        // Assert
        options.UseExponentialBackoff.Should().BeFalse();
    }

    [Fact]
    public void SagaOptions_CanSetAutoCompensateOnFailure()
    {
        // Arrange
        var options = new SagaOptions();

        // Act
        options.AutoCompensateOnFailure = false;

        // Assert
        options.AutoCompensateOnFailure.Should().BeFalse();
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
        options.StepTimeout.Should().Be(timeout);
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
        options.PersistenceInterval.Should().Be(interval);
    }
}
