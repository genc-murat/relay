using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Events;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaOptionsAndEventsTests
{
    #region SagaOptions Tests

    [Fact]
    public void SagaOptions_ShouldHaveCorrectDefaultValues()
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
    }

    [Fact]
    public void SagaOptions_ShouldAllowCustomization()
    {
        // Arrange & Act
        var options = new SagaOptions
        {
            Enabled = false,
            DefaultTimeout = TimeSpan.FromHours(1),
            AutoPersist = false,
            PersistenceInterval = TimeSpan.FromSeconds(10),
            AutoRetryFailedSteps = true,
            MaxRetryAttempts = 5,
            RetryDelay = TimeSpan.FromSeconds(10),
            UseExponentialBackoff = false,
            AutoCompensateOnFailure = false,
            ContinueCompensationOnError = false,
            StepTimeout = TimeSpan.FromMinutes(5),
            CompensationTimeout = TimeSpan.FromMinutes(10),
            TrackMetrics = false,
            EnableTelemetry = false
        };

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(TimeSpan.FromHours(1), options.DefaultTimeout);
        Assert.False(options.AutoPersist);
        Assert.Equal(TimeSpan.FromSeconds(10), options.PersistenceInterval);
        Assert.True(options.AutoRetryFailedSteps);
        Assert.Equal(5, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(10), options.RetryDelay);
        Assert.False(options.UseExponentialBackoff);
        Assert.False(options.AutoCompensateOnFailure);
        Assert.False(options.ContinueCompensationOnError);
        Assert.Equal(TimeSpan.FromMinutes(5), options.StepTimeout);
        Assert.Equal(TimeSpan.FromMinutes(10), options.CompensationTimeout);
        Assert.False(options.TrackMetrics);
        Assert.False(options.EnableTelemetry);
    }

    [Fact]
    public void SagaOptions_OnSagaCompleted_ShouldBeInvokable()
    {
        // Arrange
        var invoked = false;
        SagaCompletedEventArgs? capturedArgs = null;
        var options = new SagaOptions
        {
            OnSagaCompleted = args =>
            {
                invoked = true;
                capturedArgs = args;
            }
        };

        var eventArgs = new SagaCompletedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "test-123",
            StepsExecuted = 5,
            Duration = TimeSpan.FromSeconds(10)
        };

        // Act
        options.OnSagaCompleted?.Invoke(eventArgs);

        // Assert
        Assert.True(invoked);
        Assert.NotNull(capturedArgs);
        Assert.Equal(eventArgs.SagaId, capturedArgs!.SagaId);
        Assert.Equal(eventArgs.CorrelationId, capturedArgs.CorrelationId);
    }

    [Fact]
    public void SagaOptions_OnSagaFailed_ShouldBeInvokable()
    {
        // Arrange
        var invoked = false;
        SagaFailedEventArgs? capturedArgs = null;
        var options = new SagaOptions
        {
            OnSagaFailed = args =>
            {
                invoked = true;
                capturedArgs = args;
            }
        };

        var eventArgs = new SagaFailedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "test-456",
            FailedStep = "PaymentStep",
            Exception = new InvalidOperationException("Payment failed"),
            StepsExecutedBeforeFailure = 2
        };

        // Act
        options.OnSagaFailed?.Invoke(eventArgs);

        // Assert
        Assert.True(invoked);
        Assert.NotNull(capturedArgs);
        Assert.Equal("PaymentStep", capturedArgs!.FailedStep);
        Assert.NotNull(capturedArgs.Exception);
    }

    [Fact]
    public void SagaOptions_OnSagaCompensated_ShouldBeInvokable()
    {
        // Arrange
        var invoked = false;
        SagaCompensatedEventArgs? capturedArgs = null;
        var options = new SagaOptions
        {
            OnSagaCompensated = args =>
            {
                invoked = true;
                capturedArgs = args;
            }
        };

        var eventArgs = new SagaCompensatedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "test-789",
            CompensationSucceeded = true,
            StepsCompensated = 3
        };

        // Act
        options.OnSagaCompensated?.Invoke(eventArgs);

        // Assert
        Assert.True(invoked);
        Assert.NotNull(capturedArgs);
        Assert.True(capturedArgs!.CompensationSucceeded);
        Assert.Equal(3, capturedArgs.StepsCompensated);
    }

    #endregion

    #region SagaCompletedEventArgs Tests

    [Fact]
    public void SagaCompletedEventArgs_ShouldHaveDefaultValues()
    {
        // Act
        var eventArgs = new SagaCompletedEventArgs();

        // Assert
        Assert.Equal(Guid.Empty, eventArgs.SagaId);
        Assert.Equal(string.Empty, eventArgs.CorrelationId);
        Assert.Equal(0, eventArgs.StepsExecuted);
        Assert.Equal(TimeSpan.Zero, eventArgs.Duration);
        Assert.True((eventArgs.CompletedAt - DateTimeOffset.UtcNow).Duration() <= TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SagaCompletedEventArgs_ShouldAllowInitialization()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var correlationId = "corr-123";
        var stepsExecuted = 10;
        var duration = TimeSpan.FromMinutes(5);
        var completedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        // Act
        var eventArgs = new SagaCompletedEventArgs
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            StepsExecuted = stepsExecuted,
            Duration = duration,
            CompletedAt = completedAt
        };

        // Assert
        Assert.Equal(sagaId, eventArgs.SagaId);
        Assert.Equal(correlationId, eventArgs.CorrelationId);
        Assert.Equal(stepsExecuted, eventArgs.StepsExecuted);
        Assert.Equal(duration, eventArgs.Duration);
        Assert.Equal(completedAt, eventArgs.CompletedAt);
    }

    [Fact]
    public void SagaCompletedEventArgs_WithMultipleInstances_ShouldBeIndependent()
    {
        // Arrange & Act
        var eventArgs1 = new SagaCompletedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "saga-1",
            StepsExecuted = 3
        };

        var eventArgs2 = new SagaCompletedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "saga-2",
            StepsExecuted = 5
        };

        // Assert
        Assert.NotEqual(eventArgs2.SagaId, eventArgs1.SagaId);
        Assert.NotEqual(eventArgs2.CorrelationId, eventArgs1.CorrelationId);
        Assert.NotEqual(eventArgs2.StepsExecuted, eventArgs1.StepsExecuted);
    }

    #endregion

    #region SagaFailedEventArgs Tests

    [Fact]
    public void SagaFailedEventArgs_ShouldHaveDefaultValues()
    {
        // Act
        var eventArgs = new SagaFailedEventArgs();

        // Assert
        Assert.Equal(Guid.Empty, eventArgs.SagaId);
        Assert.Equal(string.Empty, eventArgs.CorrelationId);
        Assert.Equal(string.Empty, eventArgs.FailedStep);
        Assert.Null(eventArgs.Exception);
        Assert.True((eventArgs.FailedAt - DateTimeOffset.UtcNow).Duration() <= TimeSpan.FromSeconds(1));
        Assert.Equal(0, eventArgs.StepsExecutedBeforeFailure);
    }

    [Fact]
    public void SagaFailedEventArgs_ShouldAllowInitialization()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var correlationId = "corr-456";
        var failedStep = "ProcessPayment";
        var exception = new InvalidOperationException("Payment failed");
        var failedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var stepsExecuted = 3;

        // Act
        var eventArgs = new SagaFailedEventArgs
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            FailedStep = failedStep,
            Exception = exception,
            FailedAt = failedAt,
            StepsExecutedBeforeFailure = stepsExecuted
        };

        // Assert
        Assert.Equal(sagaId, eventArgs.SagaId);
        Assert.Equal(correlationId, eventArgs.CorrelationId);
        Assert.Equal(failedStep, eventArgs.FailedStep);
        Assert.Equal(exception, eventArgs.Exception);
        Assert.Equal(failedAt, eventArgs.FailedAt);
        Assert.Equal(stepsExecuted, eventArgs.StepsExecutedBeforeFailure);
    }

    [Fact]
    public void SagaFailedEventArgs_WithException_ShouldPreserveExceptionDetails()
    {
        // Arrange
        var innerException = new ArgumentNullException("parameter");
        var outerException = new InvalidOperationException("Operation failed", innerException);

        // Act
        var eventArgs = new SagaFailedEventArgs
        {
            SagaId = Guid.NewGuid(),
            FailedStep = "TestStep",
            Exception = outerException
        };

        // Assert
        Assert.Equal(outerException, eventArgs.Exception);
        Assert.Contains("Operation failed", eventArgs.Exception!.Message);
        Assert.Equal(innerException, eventArgs.Exception.InnerException);
    }

    [Fact]
    public void SagaFailedEventArgs_WithoutException_ShouldAllowNull()
    {
        // Act
        var eventArgs = new SagaFailedEventArgs
        {
            SagaId = Guid.NewGuid(),
            FailedStep = "TestStep",
            Exception = null
        };

        // Assert
        Assert.Null(eventArgs.Exception);
    }

    #endregion

    #region SagaCompensatedEventArgs Tests

    [Fact]
    public void SagaCompensatedEventArgs_ShouldHaveDefaultValues()
    {
        // Act
        var eventArgs = new SagaCompensatedEventArgs();

        // Assert
        Assert.Equal(Guid.Empty, eventArgs.SagaId);
        Assert.Equal(string.Empty, eventArgs.CorrelationId);
        Assert.False(eventArgs.CompensationSucceeded);
        Assert.Equal(0, eventArgs.StepsCompensated);
        Assert.True((eventArgs.CompensatedAt - DateTimeOffset.UtcNow).Duration() <= TimeSpan.FromSeconds(1));
        Assert.Null(eventArgs.OriginalException);
    }

    [Fact]
    public void SagaCompensatedEventArgs_ShouldAllowInitialization()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var correlationId = "corr-789";
        var compensationSucceeded = true;
        var stepsCompensated = 5;
        var compensatedAt = DateTimeOffset.UtcNow.AddMinutes(-3);
        var originalException = new InvalidOperationException("Original failure");

        // Act
        var eventArgs = new SagaCompensatedEventArgs
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            CompensationSucceeded = compensationSucceeded,
            StepsCompensated = stepsCompensated,
            CompensatedAt = compensatedAt,
            OriginalException = originalException
        };

        // Assert
        Assert.Equal(sagaId, eventArgs.SagaId);
        Assert.Equal(correlationId, eventArgs.CorrelationId);
        Assert.True(eventArgs.CompensationSucceeded);
        Assert.Equal(stepsCompensated, eventArgs.StepsCompensated);
        Assert.Equal(compensatedAt, eventArgs.CompensatedAt);
        Assert.Equal(originalException, eventArgs.OriginalException);
    }

    [Fact]
    public void SagaCompensatedEventArgs_WithSuccessfulCompensation_ShouldSetFlag()
    {
        // Act
        var eventArgs = new SagaCompensatedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CompensationSucceeded = true,
            StepsCompensated = 3
        };

        // Assert
        Assert.True(eventArgs.CompensationSucceeded);
        Assert.Equal(3, eventArgs.StepsCompensated);
    }

    [Fact]
    public void SagaCompensatedEventArgs_WithFailedCompensation_ShouldSetFlag()
    {
        // Act
        var eventArgs = new SagaCompensatedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CompensationSucceeded = false,
            StepsCompensated = 2,
            OriginalException = new Exception("Compensation failed")
        };

        // Assert
        Assert.False(eventArgs.CompensationSucceeded);
        Assert.Equal(2, eventArgs.StepsCompensated);
        Assert.NotNull(eventArgs.OriginalException);
    }

    [Fact]
    public void SagaCompensatedEventArgs_WithZeroSteps_ShouldBeValid()
    {
        // Act
        var eventArgs = new SagaCompensatedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CompensationSucceeded = true,
            StepsCompensated = 0 // No steps to compensate
        };

        // Assert
        Assert.Equal(0, eventArgs.StepsCompensated);
        Assert.True(eventArgs.CompensationSucceeded);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SagaOptions_WithAllEventHandlers_ShouldWorkTogether()
    {
        // Arrange
        var completedInvoked = false;
        var failedInvoked = false;
        var compensatedInvoked = false;

        var options = new SagaOptions
        {
            OnSagaCompleted = args => completedInvoked = true,
            OnSagaFailed = args => failedInvoked = true,
            OnSagaCompensated = args => compensatedInvoked = true
        };

        // Act
        options.OnSagaCompleted?.Invoke(new SagaCompletedEventArgs { SagaId = Guid.NewGuid() });
        options.OnSagaFailed?.Invoke(new SagaFailedEventArgs { SagaId = Guid.NewGuid() });
        options.OnSagaCompensated?.Invoke(new SagaCompensatedEventArgs { SagaId = Guid.NewGuid() });

        // Assert
        Assert.True(completedInvoked);
        Assert.True(failedInvoked);
        Assert.True(compensatedInvoked);
    }

    [Fact]
    public void EventArgs_ShouldSupportCompleteWorkflow()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var correlationId = "workflow-123";

        // Simulate saga lifecycle
        var failedArgs = new SagaFailedEventArgs
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            FailedStep = "Step3",
            StepsExecutedBeforeFailure = 2,
            Exception = new Exception("Step failed")
        };

        var compensatedArgs = new SagaCompensatedEventArgs
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            CompensationSucceeded = true,
            StepsCompensated = 2,
            OriginalException = failedArgs.Exception
        };

        // Assert - Events are linked by saga ID and correlation ID
        Assert.Equal(failedArgs.SagaId, compensatedArgs.SagaId);
        Assert.Equal(failedArgs.CorrelationId, compensatedArgs.CorrelationId);
        Assert.Equal(failedArgs.StepsExecutedBeforeFailure, compensatedArgs.StepsCompensated);
        Assert.Equal(failedArgs.Exception, compensatedArgs.OriginalException);
    }

    #endregion
}
