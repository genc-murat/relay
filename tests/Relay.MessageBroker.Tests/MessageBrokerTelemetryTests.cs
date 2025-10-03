using System.Diagnostics;
using Relay.MessageBroker;
using Relay.MessageBroker.Telemetry;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerTelemetryTests
{
    [Fact]
    public void ActivitySourceName_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("Relay.MessageBroker", MessageBrokerTelemetry.ActivitySourceName);
    }

    [Fact]
    public void ActivitySource_ShouldBeCreated()
    {
        // Assert
        Assert.NotNull(MessageBrokerTelemetry.ActivitySource);
        Assert.Equal("Relay.MessageBroker", MessageBrokerTelemetry.ActivitySource.Name);
    }

    [Fact]
    public void ActivitySourceVersion_ShouldBeSet()
    {
        // Assert
        Assert.Equal("1.0.0", MessageBrokerTelemetry.ActivitySourceVersion);
    }

    [Fact]
    public void MeterName_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("Relay.MessageBroker", MessageBrokerTelemetry.MeterName);
    }

    [Fact]
    public void MeterVersion_ShouldBeSet()
    {
        // Assert
        Assert.Equal("1.0.0", MessageBrokerTelemetry.MeterVersion);
    }

    [Fact]
    public void Attributes_ShouldContainMessagingAttributes()
    {
        // Assert
        Assert.Equal("messaging.system", MessageBrokerTelemetry.Attributes.MessagingSystem);
        Assert.Equal("messaging.destination", MessageBrokerTelemetry.Attributes.MessagingDestination);
        Assert.Equal("messaging.operation", MessageBrokerTelemetry.Attributes.MessagingOperation);
    }

    [Fact]
    public void Attributes_ShouldContainCustomAttributes()
    {
        // Assert
        Assert.Equal("relay.message.type", MessageBrokerTelemetry.Attributes.MessageType);
        Assert.Equal("relay.message.compressed", MessageBrokerTelemetry.Attributes.MessageCompressed);
        Assert.Equal("relay.circuit_breaker.state", MessageBrokerTelemetry.Attributes.CircuitBreakerState);
    }

    [Fact]
    public void Events_ShouldContainExpectedEvents()
    {
        // Assert
        Assert.Equal("message.published", MessageBrokerTelemetry.Events.MessagePublished);
        Assert.Equal("message.received", MessageBrokerTelemetry.Events.MessageReceived);
        Assert.Equal("message.processed", MessageBrokerTelemetry.Events.MessageProcessed);
        Assert.Equal("circuit_breaker.opened", MessageBrokerTelemetry.Events.CircuitBreakerOpened);
    }

    [Fact]
    public void Metrics_ShouldContainCounterMetrics()
    {
        // Assert
        Assert.Equal("relay.messages.published", MessageBrokerTelemetry.Metrics.MessagesPublished);
        Assert.Equal("relay.messages.received", MessageBrokerTelemetry.Metrics.MessagesReceived);
        Assert.Equal("relay.messages.failed", MessageBrokerTelemetry.Metrics.MessagesFailed);
    }

    [Fact]
    public void Metrics_ShouldContainHistogramMetrics()
    {
        // Assert
        Assert.Equal("relay.message.publish.duration", MessageBrokerTelemetry.Metrics.MessagePublishDuration);
        Assert.Equal("relay.message.process.duration", MessageBrokerTelemetry.Metrics.MessageProcessDuration);
        Assert.Equal("relay.message.payload.size", MessageBrokerTelemetry.Metrics.MessagePayloadSize);
    }

    [Fact]
    public void Metrics_ShouldContainGaugeMetrics()
    {
        // Assert
        Assert.Equal("relay.circuit_breaker.state", MessageBrokerTelemetry.Metrics.CircuitBreakerState);
        Assert.Equal("relay.connections.active", MessageBrokerTelemetry.Metrics.ActiveConnections);
        Assert.Equal("relay.queue.size", MessageBrokerTelemetry.Metrics.QueueSize);
    }

    [Fact]
    public void ActivitySource_ShouldCreateActivity()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        
        // Act
        using var activity = MessageBrokerTelemetry.ActivitySource.StartActivity("test-activity");
        
        // Assert
        Assert.NotNull(activity);
        Assert.Equal("test-activity", activity.OperationName);
    }

    [Fact]
    public void ActivitySource_WithTags_ShouldSetTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        
        // Act
        using var activity = MessageBrokerTelemetry.ActivitySource.StartActivity("test-activity");
        activity?.SetTag(MessageBrokerTelemetry.Attributes.MessageType, "TestMessage");
        activity?.SetTag(MessageBrokerTelemetry.Attributes.MessagingDestination, "test.queue");
        
        // Assert
        Assert.NotNull(activity);
        var messageTypeTag = activity.Tags.FirstOrDefault(t => t.Key == MessageBrokerTelemetry.Attributes.MessageType);
        var destinationTag = activity.Tags.FirstOrDefault(t => t.Key == MessageBrokerTelemetry.Attributes.MessagingDestination);
        
        Assert.Equal("TestMessage", messageTypeTag.Value);
        Assert.Equal("test.queue", destinationTag.Value);
    }
}
