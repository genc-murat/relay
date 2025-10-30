using Relay.MessageBroker.Backpressure;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BackpressureEventTests
{
    [Fact]
    public void BackpressureEvent_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var backpressureEvent = new BackpressureEvent();

        // Assert
        Assert.Equal(BackpressureEventType.Activated, backpressureEvent.EventType);
        Assert.Equal(default(DateTimeOffset), backpressureEvent.Timestamp);
        Assert.Equal(TimeSpan.Zero, backpressureEvent.AverageLatency);
        Assert.Equal(0, backpressureEvent.QueueDepth);
        Assert.Equal(string.Empty, backpressureEvent.Reason);
    }

    [Fact]
    public void BackpressureEvent_WithValues_ShouldStoreCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var averageLatency = TimeSpan.FromMilliseconds(250);

        // Act
        var backpressureEvent = new BackpressureEvent
        {
            EventType = BackpressureEventType.Deactivated,
            Timestamp = timestamp,
            AverageLatency = averageLatency,
            QueueDepth = 1000,
            Reason = "Conditions improved"
        };

        // Assert
        Assert.Equal(BackpressureEventType.Deactivated, backpressureEvent.EventType);
        Assert.Equal(timestamp, backpressureEvent.Timestamp);
        Assert.Equal(averageLatency, backpressureEvent.AverageLatency);
        Assert.Equal(1000, backpressureEvent.QueueDepth);
        Assert.Equal("Conditions improved", backpressureEvent.Reason);
    }
}

public class BackpressureEventTypeTests
{
    [Fact]
    public void BackpressureEventType_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)BackpressureEventType.Activated);
        Assert.Equal(1, (int)BackpressureEventType.Deactivated);
    }
}