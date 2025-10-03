using FluentAssertions;
using Relay.MessageBroker.Saga;
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
        options.Enabled.Should().BeFalse();
        options.DefaultTimeout.Should().Be(TimeSpan.FromHours(1));
        options.AutoPersist.Should().BeFalse();
        options.PersistenceInterval.Should().Be(TimeSpan.FromSeconds(10));
        options.AutoRetryFailedSteps.Should().BeTrue();
        options.MaxRetryAttempts.Should().Be(5);
        options.RetryDelay.Should().Be(TimeSpan.FromSeconds(10));
        options.UseExponentialBackoff.Should().BeFalse();
        options.AutoCompensateOnFailure.Should().BeFalse();
        options.ContinueCompensationOnError.Should().BeFalse();
        options.StepTimeout.Should().Be(TimeSpan.FromMinutes(5));
        options.CompensationTimeout.Should().Be(TimeSpan.FromMinutes(10));
        options.TrackMetrics.Should().BeFalse();
        options.EnableTelemetry.Should().BeFalse();
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
        invoked.Should().BeTrue();
        capturedArgs.Should().NotBeNull();
        capturedArgs!.SagaId.Should().Be(eventArgs.SagaId);
        capturedArgs.CorrelationId.Should().Be(eventArgs.CorrelationId);
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
        invoked.Should().BeTrue();
        capturedArgs.Should().NotBeNull();
        capturedArgs!.FailedStep.Should().Be("PaymentStep");
        capturedArgs.Exception.Should().NotBeNull();
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
        invoked.Should().BeTrue();
        capturedArgs.Should().NotBeNull();
        capturedArgs!.CompensationSucceeded.Should().BeTrue();
        capturedArgs.StepsCompensated.Should().Be(3);
    }

    #endregion

    #region SagaCompletedEventArgs Tests

    [Fact]
    public void SagaCompletedEventArgs_ShouldHaveDefaultValues()
    {
        // Act
        var eventArgs = new SagaCompletedEventArgs();

        // Assert
        eventArgs.SagaId.Should().Be(Guid.Empty);
        eventArgs.CorrelationId.Should().Be(string.Empty);
        eventArgs.StepsExecuted.Should().Be(0);
        eventArgs.Duration.Should().Be(TimeSpan.Zero);
        eventArgs.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
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
        eventArgs.SagaId.Should().Be(sagaId);
        eventArgs.CorrelationId.Should().Be(correlationId);
        eventArgs.StepsExecuted.Should().Be(stepsExecuted);
        eventArgs.Duration.Should().Be(duration);
        eventArgs.CompletedAt.Should().Be(completedAt);
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
        eventArgs1.SagaId.Should().NotBe(eventArgs2.SagaId);
        eventArgs1.CorrelationId.Should().NotBe(eventArgs2.CorrelationId);
        eventArgs1.StepsExecuted.Should().NotBe(eventArgs2.StepsExecuted);
    }

    #endregion

    #region SagaFailedEventArgs Tests

    [Fact]
    public void SagaFailedEventArgs_ShouldHaveDefaultValues()
    {
        // Act
        var eventArgs = new SagaFailedEventArgs();

        // Assert
        eventArgs.SagaId.Should().Be(Guid.Empty);
        eventArgs.CorrelationId.Should().Be(string.Empty);
        eventArgs.FailedStep.Should().Be(string.Empty);
        eventArgs.Exception.Should().BeNull();
        eventArgs.FailedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        eventArgs.StepsExecutedBeforeFailure.Should().Be(0);
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
        eventArgs.SagaId.Should().Be(sagaId);
        eventArgs.CorrelationId.Should().Be(correlationId);
        eventArgs.FailedStep.Should().Be(failedStep);
        eventArgs.Exception.Should().Be(exception);
        eventArgs.FailedAt.Should().Be(failedAt);
        eventArgs.StepsExecutedBeforeFailure.Should().Be(stepsExecuted);
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
        eventArgs.Exception.Should().Be(outerException);
        eventArgs.Exception!.Message.Should().Contain("Operation failed");
        eventArgs.Exception.InnerException.Should().Be(innerException);
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
        eventArgs.Exception.Should().BeNull();
    }

    #endregion

    #region SagaCompensatedEventArgs Tests

    [Fact]
    public void SagaCompensatedEventArgs_ShouldHaveDefaultValues()
    {
        // Act
        var eventArgs = new SagaCompensatedEventArgs();

        // Assert
        eventArgs.SagaId.Should().Be(Guid.Empty);
        eventArgs.CorrelationId.Should().Be(string.Empty);
        eventArgs.CompensationSucceeded.Should().BeFalse();
        eventArgs.StepsCompensated.Should().Be(0);
        eventArgs.CompensatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        eventArgs.OriginalException.Should().BeNull();
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
        eventArgs.SagaId.Should().Be(sagaId);
        eventArgs.CorrelationId.Should().Be(correlationId);
        eventArgs.CompensationSucceeded.Should().BeTrue();
        eventArgs.StepsCompensated.Should().Be(stepsCompensated);
        eventArgs.CompensatedAt.Should().Be(compensatedAt);
        eventArgs.OriginalException.Should().Be(originalException);
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
        eventArgs.CompensationSucceeded.Should().BeTrue();
        eventArgs.StepsCompensated.Should().Be(3);
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
        eventArgs.CompensationSucceeded.Should().BeFalse();
        eventArgs.StepsCompensated.Should().Be(2);
        eventArgs.OriginalException.Should().NotBeNull();
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
        eventArgs.StepsCompensated.Should().Be(0);
        eventArgs.CompensationSucceeded.Should().BeTrue();
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
        completedInvoked.Should().BeTrue();
        failedInvoked.Should().BeTrue();
        compensatedInvoked.Should().BeTrue();
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
        compensatedArgs.SagaId.Should().Be(failedArgs.SagaId);
        compensatedArgs.CorrelationId.Should().Be(failedArgs.CorrelationId);
        compensatedArgs.StepsCompensated.Should().Be(failedArgs.StepsExecutedBeforeFailure);
        compensatedArgs.OriginalException.Should().Be(failedArgs.Exception);
    }

    #endregion
}
