using Relay.MessageBroker.Batch;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BatchProcessorMetricsTests
{
    [Fact]
    public void BatchProcessorMetrics_DefaultValues_ShouldBeZero()
    {
        // Act
        var metrics = new BatchProcessorMetrics();

        // Assert
        Assert.Equal(0, metrics.CurrentBatchSize);
        Assert.Equal(0L, metrics.TotalMessagesProcessed);
        Assert.Equal(0L, metrics.TotalBatchesProcessed);
        Assert.Equal(0.0, metrics.AverageBatchSize);
        Assert.Equal(0.0, metrics.AverageProcessingTimeMs);
        Assert.Equal(0.0, metrics.SuccessRate);
        Assert.Equal(0L, metrics.TotalFailedMessages);
        Assert.Equal(0.0, metrics.CompressionRatio);
        Assert.Null(metrics.LastFlushAt);
    }

    [Fact]
    public void BatchProcessorMetrics_WithValues_ShouldStoreCorrectly()
    {
        // Arrange
        var averageProcessingTime = TimeSpan.FromMilliseconds(150);

        // Act
        var lastFlushAt = DateTimeOffset.UtcNow;

        var metrics = new BatchProcessorMetrics
        {
            CurrentBatchSize = 5,
            TotalMessagesProcessed = 1000,
            TotalBatchesProcessed = 50,
            AverageBatchSize = 20.5,
            AverageProcessingTimeMs = 150.0,
            SuccessRate = 0.95,
            TotalFailedMessages = 2,
            CompressionRatio = 2.5,
            LastFlushAt = lastFlushAt
        };

        // Assert
        Assert.Equal(5, metrics.CurrentBatchSize);
        Assert.Equal(1000L, metrics.TotalMessagesProcessed);
        Assert.Equal(50L, metrics.TotalBatchesProcessed);
        Assert.Equal(20.5, metrics.AverageBatchSize);
        Assert.Equal(150.0, metrics.AverageProcessingTimeMs);
        Assert.Equal(0.95, metrics.SuccessRate);
        Assert.Equal(2L, metrics.TotalFailedMessages);
        Assert.Equal(2.5, metrics.CompressionRatio);
        Assert.Equal(lastFlushAt, metrics.LastFlushAt);
    }
}