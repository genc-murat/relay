using Relay.MessageBroker.Backpressure;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BackpressureMetricsTests
{
    [Fact]
    public void BackpressureMetrics_DefaultValues_ShouldBeZeroOrFalse()
    {
        // Act
        var metrics = new BackpressureMetrics();

        // Assert
        Assert.Equal(TimeSpan.Zero, metrics.AverageLatency);
        Assert.Equal(0, metrics.QueueDepth);
        Assert.False(metrics.IsThrottling);
        Assert.Equal(0L, metrics.TotalProcessingRecords);
        Assert.Equal(0L, metrics.BackpressureActivations);
        Assert.Null(metrics.LastBackpressureActivation);
        Assert.Null(metrics.LastBackpressureDeactivation);
        Assert.Equal(TimeSpan.Zero, metrics.MinLatency);
        Assert.Equal(TimeSpan.Zero, metrics.MaxLatency);
    }

    [Fact]
    public void BackpressureMetrics_WithValues_ShouldStoreCorrectly()
    {
        // Arrange
        var activationTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var deactivationTime = DateTimeOffset.UtcNow.AddMinutes(-1);

        // Act
        var metrics = new BackpressureMetrics
        {
            AverageLatency = TimeSpan.FromMilliseconds(150),
            QueueDepth = 500,
            IsThrottling = true,
            TotalProcessingRecords = 1000,
            BackpressureActivations = 3,
            LastBackpressureActivation = activationTime,
            LastBackpressureDeactivation = deactivationTime,
            MinLatency = TimeSpan.FromMilliseconds(50),
            MaxLatency = TimeSpan.FromMilliseconds(300)
        };

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(150), metrics.AverageLatency);
        Assert.Equal(500, metrics.QueueDepth);
        Assert.True(metrics.IsThrottling);
        Assert.Equal(1000L, metrics.TotalProcessingRecords);
        Assert.Equal(3L, metrics.BackpressureActivations);
        Assert.Equal(activationTime, metrics.LastBackpressureActivation);
        Assert.Equal(deactivationTime, metrics.LastBackpressureDeactivation);
        Assert.Equal(TimeSpan.FromMilliseconds(50), metrics.MinLatency);
        Assert.Equal(TimeSpan.FromMilliseconds(300), metrics.MaxLatency);
    }
}