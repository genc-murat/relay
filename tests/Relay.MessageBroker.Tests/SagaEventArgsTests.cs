using Relay.MessageBroker.Saga;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaEventArgsTests
{
    [Fact]
    public void SagaCompletedEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var correlationId = "CORR-123";
        var stepsExecuted = 5;
        var duration = TimeSpan.FromSeconds(10);
        var completedAt = DateTimeOffset.UtcNow;

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
    public void SagaFailedEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var correlationId = "CORR-456";
        var failedStep = "ProcessPayment";
        var exception = new InvalidOperationException("Payment failed");
        var failedAt = DateTimeOffset.UtcNow;
        var stepsExecuted = 2;

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
    public void SagaCompensatedEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var correlationId = "CORR-789";
        var compensationSucceeded = true;
        var stepsCompensated = 3;
        var compensatedAt = DateTimeOffset.UtcNow;
        var originalException = new InvalidOperationException("Original error");

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
    public void SagaCompletedEventArgs_WithDefaultValues_ShouldWork()
    {
        // Act
        var eventArgs = new SagaCompletedEventArgs();

        // Assert
        Assert.Equal(Guid.Empty, eventArgs.SagaId);
        Assert.Empty(eventArgs.CorrelationId);
        Assert.Equal(0, eventArgs.StepsExecuted);
        Assert.Equal(TimeSpan.Zero, eventArgs.Duration);
        Assert.True((eventArgs.CompletedAt - DateTimeOffset.UtcNow).Duration() <= TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SagaFailedEventArgs_WithDefaultValues_ShouldWork()
    {
        // Act
        var eventArgs = new SagaFailedEventArgs();

        // Assert
        Assert.Equal(Guid.Empty, eventArgs.SagaId);
        Assert.Empty(eventArgs.CorrelationId);
        Assert.Empty(eventArgs.FailedStep);
        Assert.Null(eventArgs.Exception);
        Assert.True((eventArgs.FailedAt - DateTimeOffset.UtcNow).Duration() <= TimeSpan.FromSeconds(5));
        Assert.Equal(0, eventArgs.StepsExecutedBeforeFailure);
    }

    [Fact]
    public void SagaCompensatedEventArgs_WithDefaultValues_ShouldWork()
    {
        // Act
        var eventArgs = new SagaCompensatedEventArgs();

        // Assert
        Assert.Equal(Guid.Empty, eventArgs.SagaId);
        Assert.Empty(eventArgs.CorrelationId);
        Assert.False(eventArgs.CompensationSucceeded);
        Assert.Equal(0, eventArgs.StepsCompensated);
        Assert.True((eventArgs.CompensatedAt - DateTimeOffset.UtcNow).Duration() <= TimeSpan.FromSeconds(5));
        Assert.Null(eventArgs.OriginalException);
    }

    [Fact]
    public void SagaFailedEventArgs_WithNullException_ShouldWork()
    {
        // Act
        var eventArgs = new SagaFailedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CORR-NULL",
            FailedStep = "TestStep",
            Exception = null
        };

        // Assert
        Assert.Null(eventArgs.Exception);
    }

    [Fact]
    public void SagaCompensatedEventArgs_WithNullOriginalException_ShouldWork()
    {
        // Act
        var eventArgs = new SagaCompensatedEventArgs
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CORR-NULL",
            CompensationSucceeded = true,
            OriginalException = null
        };

        // Assert
        Assert.Null(eventArgs.OriginalException);
    }
}
