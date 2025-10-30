using Relay.MessageBroker.Bulkhead;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BulkheadMetricsTests
{
    [Fact]
    public void BulkheadMetrics_DefaultValues_ShouldBeZero()
    {
        // Act
        var metrics = new BulkheadMetrics();

        // Assert
        Assert.Equal(0, metrics.ActiveOperations);
        Assert.Equal(0, metrics.QueuedOperations);
        Assert.Equal(0L, metrics.RejectedOperations);
        Assert.Equal(0L, metrics.ExecutedOperations);
        Assert.Equal(0, metrics.MaxConcurrentOperations);
        Assert.Equal(0, metrics.MaxQueuedOperations);
        Assert.Equal(TimeSpan.Zero, metrics.AverageWaitTime);
    }

    [Fact]
    public void BulkheadMetrics_WithValues_ShouldStoreCorrectly()
    {
        // Arrange
        var averageWaitTime = TimeSpan.FromMilliseconds(150);

        // Act
        var metrics = new BulkheadMetrics
        {
            ActiveOperations = 5,
            QueuedOperations = 10,
            RejectedOperations = 25,
            ExecutedOperations = 1000,
            MaxConcurrentOperations = 20,
            MaxQueuedOperations = 50,
            AverageWaitTime = averageWaitTime
        };

        // Assert
        Assert.Equal(5, metrics.ActiveOperations);
        Assert.Equal(10, metrics.QueuedOperations);
        Assert.Equal(25L, metrics.RejectedOperations);
        Assert.Equal(1000L, metrics.ExecutedOperations);
        Assert.Equal(20, metrics.MaxConcurrentOperations);
        Assert.Equal(50, metrics.MaxQueuedOperations);
        Assert.Equal(averageWaitTime, metrics.AverageWaitTime);
    }
}