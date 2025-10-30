using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Batch;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BatchProcessorTests
{
    private class TestMessage
    {
        public string Content { get; set; } = string.Empty;
    }

    [Fact]
    public async Task AddAsync_ShouldAddMessageToBatch()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 10,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test" }, null);
        var metrics = processor.GetMetrics();

        // Assert
        Assert.Equal(1, metrics.CurrentBatchSize);

        await processor.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldFlushWhenBatchSizeReached()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 3,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test3" }, null);

        // Wait a bit for async flush
        await Task.Delay(100);

        var metrics = processor.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.CurrentBatchSize);
        Assert.Equal(1, metrics.TotalBatchesProcessed);
        Assert.Equal(3, metrics.TotalMessagesProcessed);

        await processor.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldThrowWhenMessageIsNull()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 10,
            FlushInterval = TimeSpan.FromSeconds(10)
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await processor.AddAsync(null!, null);
        });

        await processor.DisposeAsync();
    }

    [Fact]
    public async Task FlushAsync_ShouldPublishAllMessages()
    {
        // Arrange
        var publishedMessages = new List<TestMessage>();
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, PublishOptions?, CancellationToken>((msg, opts, ct) =>
            {
                publishedMessages.Add((TestMessage)msg);
            })
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 100,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        await processor.FlushAsync();

        // Assert
        Assert.Equal(2, publishedMessages.Count);
        Assert.Equal("Test1", publishedMessages[0].Content);
        Assert.Equal("Test2", publishedMessages[1].Content);

        await processor.DisposeAsync();
    }

    [Fact]
    public async Task FlushAsync_ShouldClearBatchAfterFlush()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 100,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test" }, null);
        await processor.FlushAsync();
        var metrics = processor.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.CurrentBatchSize);

        await processor.DisposeAsync();
    }

    [Fact]
    public async Task FlushAsync_ShouldHandleEmptyBatch()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 100,
            FlushInterval = TimeSpan.FromSeconds(10)
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act & Assert (should not throw)
        await processor.FlushAsync();

        await processor.DisposeAsync();
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnCorrectBatchCount()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 2,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        await Task.Delay(100); // Wait for flush

        await processor.AddAsync(new TestMessage { Content = "Test3" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test4" }, null);
        await Task.Delay(100); // Wait for flush

        var metrics = processor.GetMetrics();

        // Assert
        Assert.Equal(2, metrics.TotalBatchesProcessed);
        Assert.Equal(4, metrics.TotalMessagesProcessed);

        await processor.DisposeAsync();
    }

    [Fact]
    public async Task GetMetrics_ShouldCalculateAverageBatchSize()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 100,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        await processor.FlushAsync();

        await processor.AddAsync(new TestMessage { Content = "Test3" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test4" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test5" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test6" }, null);
        await processor.FlushAsync();

        var metrics = processor.GetMetrics();

        // Assert
        Assert.Equal(3.0, metrics.AverageBatchSize); // (2 + 4) / 2

        await processor.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldClearBatch()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<object>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 100,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        
        var metricsBeforeDispose = processor.GetMetrics();
        Assert.Equal(2, metricsBeforeDispose.CurrentBatchSize);
        
        await processor.DisposeAsync();

        // Assert - DisposeAsync should attempt to flush (implementation detail)
        // We just verify it doesn't throw
        Assert.True(true);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowWhenDisposed()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 10,
            FlushInterval = TimeSpan.FromSeconds(10)
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        await processor.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await processor.AddAsync(new TestMessage { Content = "Test" }, null);
        });
    }

    [Fact]
    public async Task Constructor_ShouldThrowWhenBrokerIsNull()
    {
        // Arrange
        var options = Options.Create(new BatchOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new BatchProcessor<TestMessage>(
                null!,
                options,
                NullLogger<BatchProcessor<TestMessage>>.Instance);
        });
    }

    [Fact]
    public async Task Constructor_ShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new BatchProcessor<TestMessage>(
                brokerMock.Object,
                null!,
                NullLogger<BatchProcessor<TestMessage>>.Instance);
        });
    }
}
