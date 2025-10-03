using FluentAssertions;
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
        eventArgs.SagaId.Should().Be(sagaId);
        eventArgs.CorrelationId.Should().Be(correlationId);
        eventArgs.StepsExecuted.Should().Be(stepsExecuted);
        eventArgs.Duration.Should().Be(duration);
        eventArgs.CompletedAt.Should().Be(completedAt);
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
        eventArgs.SagaId.Should().Be(sagaId);
        eventArgs.CorrelationId.Should().Be(correlationId);
        eventArgs.FailedStep.Should().Be(failedStep);
        eventArgs.Exception.Should().Be(exception);
        eventArgs.FailedAt.Should().Be(failedAt);
        eventArgs.StepsExecutedBeforeFailure.Should().Be(stepsExecuted);
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
        eventArgs.SagaId.Should().Be(sagaId);
        eventArgs.CorrelationId.Should().Be(correlationId);
        eventArgs.CompensationSucceeded.Should().BeTrue();
        eventArgs.StepsCompensated.Should().Be(stepsCompensated);
        eventArgs.CompensatedAt.Should().Be(compensatedAt);
        eventArgs.OriginalException.Should().Be(originalException);
    }

    [Fact]
    public void SagaCompletedEventArgs_WithDefaultValues_ShouldWork()
    {
        // Act
        var eventArgs = new SagaCompletedEventArgs();

        // Assert
        eventArgs.SagaId.Should().Be(Guid.Empty);
        eventArgs.CorrelationId.Should().BeEmpty();
        eventArgs.StepsExecuted.Should().Be(0);
        eventArgs.Duration.Should().Be(TimeSpan.Zero);
        eventArgs.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SagaFailedEventArgs_WithDefaultValues_ShouldWork()
    {
        // Act
        var eventArgs = new SagaFailedEventArgs();

        // Assert
        eventArgs.SagaId.Should().Be(Guid.Empty);
        eventArgs.CorrelationId.Should().BeEmpty();
        eventArgs.FailedStep.Should().BeEmpty();
        eventArgs.Exception.Should().BeNull();
        eventArgs.FailedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        eventArgs.StepsExecutedBeforeFailure.Should().Be(0);
    }

    [Fact]
    public void SagaCompensatedEventArgs_WithDefaultValues_ShouldWork()
    {
        // Act
        var eventArgs = new SagaCompensatedEventArgs();

        // Assert
        eventArgs.SagaId.Should().Be(Guid.Empty);
        eventArgs.CorrelationId.Should().BeEmpty();
        eventArgs.CompensationSucceeded.Should().BeFalse();
        eventArgs.StepsCompensated.Should().Be(0);
        eventArgs.CompensatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        eventArgs.OriginalException.Should().BeNull();
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
        eventArgs.Exception.Should().BeNull();
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
        eventArgs.OriginalException.Should().BeNull();
    }
}
