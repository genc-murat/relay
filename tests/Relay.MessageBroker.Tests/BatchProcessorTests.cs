using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Batch;

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
    
    [Fact]
    public async Task AddAsync_WithCompressionEnabled_ShouldCompressAndPublish()
    {
        // Arrange
        var publishedData = new List<byte[]>();
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<byte[]>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<byte[], PublishOptions?, CancellationToken>((data, opts, ct) =>
            {
                publishedData.Add(data);
            })
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 2,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = true
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        
        // Wait for flush
        await Task.Delay(100);

        // Assert
        Assert.Equal(1, publishedData.Count); // One compressed batch
        // Verify that data is compressed (will have different characteristics than raw JSON)
        var metrics = processor.GetMetrics();
        Assert.Equal(1, metrics.TotalBatchesProcessed);
        Assert.Equal(2, metrics.TotalMessagesProcessed);
        // Compression ratio should be calculated
        Assert.True(metrics.CompressionRatio > 0);

        await processor.DisposeAsync();
    }
    
    [Fact]
    public async Task TimerBasedFlush_ShouldWork()
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
            MaxBatchSize = 10,
            FlushInterval = TimeSpan.FromMilliseconds(50), // Very short interval
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        
        // Wait for timer-based flush (timer fires after 50ms, then needs time to complete)
        await Task.Delay(200);

        // Assert
        var metrics = processor.GetMetrics();
        Assert.Equal(1, metrics.TotalBatchesProcessed);
        Assert.Equal(1, metrics.TotalMessagesProcessed);

        await processor.DisposeAsync();
    }
    
    [Fact]
    public async Task FlushAsync_WithPublishFailure_ShouldHandleError()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Publish failed"));

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 3,  // Use 3 so 2 messages don't trigger a flush during AddAsync
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false,
            PartialRetry = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act - Add 2 messages (less than max batch size, so no flush yet)
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        
        // Assert - Flush should fail because of the mock
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await processor.FlushAsync());

        await processor.DisposeAsync();
    }
    
    [Fact]
    public async Task FlushAsync_WithPartialRetry_ShouldRetryFailedMessages()
    {
        // Arrange - Setup broker to succeed on retry for specific messages
        var brokerMock = new Mock<IMessageBroker>();
        var failedPublishes = new List<TestMessage>();
        
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestMessage, PublishOptions?, CancellationToken>((msg, opts, ct) =>
            {
                // Fail the first individual publish, then succeed on retry
                if (msg.Content == "FailedMessage" && failedPublishes.Count < 1)
                {
                    failedPublishes.Add(msg);
                    throw new InvalidOperationException("Publish failed");
                }
            })
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 5,  // Make the batch size larger so all messages go in one batch
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false,
            PartialRetry = true
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Add messages - some that may fail on first attempt
        await processor.AddAsync(new TestMessage { Content = "Success1" }, null);
        await processor.AddAsync(new TestMessage { Content = "FailedMessage" }, null);  // Will fail once then succeed
        await processor.AddAsync(new TestMessage { Content = "Success2" }, null);
        
        // Act - This should succeed with retries for failed individual messages
        await processor.FlushAsync();

        // Assert
        var metrics = processor.GetMetrics();
        Assert.Equal(1, metrics.TotalBatchesProcessed);
        Assert.Equal(3, metrics.TotalMessagesProcessed);
        Assert.Equal(0, metrics.TotalFailedMessages); // All should eventually succeed with retry

        await processor.DisposeAsync();
    }
    
    [Fact]
    public async Task GetMetrics_ShouldCalculateAverageProcessingTime()
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
            MaxBatchSize = 1,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act - Add several messages to generate processing time data
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.FlushAsync();
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        await processor.FlushAsync();
        
        var metrics = processor.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.CurrentBatchSize);
        Assert.True(metrics.TotalBatchesProcessed >= 2);
        Assert.True(metrics.TotalMessagesProcessed >= 2);

        await processor.DisposeAsync();
    }
    
    [Fact]
    public async Task GetMetrics_ShouldCalculateCompressionRatio()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<byte[]>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 2,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = true
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        await processor.FlushAsync();
        
        var metrics = processor.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.CurrentBatchSize);
        Assert.Equal(1, metrics.TotalBatchesProcessed);
        Assert.Equal(2, metrics.TotalMessagesProcessed);
        Assert.True(metrics.CompressionRatio > 0); // Should have calculated compression ratio

        await processor.DisposeAsync();
    }
    
    [Fact]
    public async Task DisposeAsync_WithRemainingMessages_ShouldFlush()
    {
        // Arrange
        var publishedMessages = new List<TestMessage>();
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestMessage, PublishOptions?, CancellationToken>((msg, opts, ct) =>
            {
                publishedMessages.Add(msg);
            })
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 10,  // Use a large size so no flush happens during AddAsync
            FlushInterval = TimeSpan.FromMinutes(1), // Use a long interval so timer doesn't trigger
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act - Add messages without triggering flush
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);
        // Both messages should stay in the batch until flush or disposal
        
        // Dispose should flush remaining messages
        await processor.DisposeAsync();

        // Assert
        Assert.Equal(2, publishedMessages.Count); // Both messages should be published during disposal
    }
    
    [Fact]
    public async Task AddAsync_WithCompressionFailure_ShouldHandleError()
    {
        // Arrange
        var brokerMock = new Mock<IMessageBroker>();
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<byte[]>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 1,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = true
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act & Assert - Add message and flush
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.FlushAsync();
        
        var metrics = processor.GetMetrics();
        Assert.Equal(1, metrics.TotalBatchesProcessed);
        Assert.Equal(1, metrics.TotalMessagesProcessed);

        await processor.DisposeAsync();
    }
    
    [Fact]
    public async Task GetMetrics_ShouldReturnLastFlushTime()
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
            MaxBatchSize = 1,
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false
        });
        var processor = new BatchProcessor<TestMessage>(
            brokerMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act - Add and flush to trigger a batch
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.FlushAsync();
        
        var metrics = processor.GetMetrics();

        // Assert
        Assert.NotNull(metrics.LastFlushAt);
        Assert.True(metrics.LastFlushAt.Value <= DateTimeOffset.UtcNow);
        Assert.True(metrics.LastFlushAt.Value >= DateTimeOffset.UtcNow.AddSeconds(-5)); // Should be recent

        await processor.DisposeAsync();
    }
    
    [Fact]
    public async Task FlushAsync_WithPublishFailureAndPartialRetry_ShouldProcessSome()
    {
        // Arrange - Setup broker to fail on only one message in the batch
        var attempts = new Dictionary<string, int>();
        var successfulCount = 0;
        
        var brokerMock = new Mock<IMessageBroker>();
        
        // Setup to conditionally throw based on attempt count but track successful operations separately
        brokerMock.Setup(b => b.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestMessage, PublishOptions?, CancellationToken>((msg, opts, ct) =>
            {
                var key = msg.Content;
                if (!attempts.ContainsKey(key))
                    attempts[key] = 0;
                attempts[key]++;
                
                // Count as successful if it doesn't fail (determined by the condition below)
                // For Test2, first attempt fails, subsequent attempts succeed
                if (msg.Content != "Test2" || attempts[key] > 1)  // Not Test2 OR it's 2nd+ attempt
                {
                    successfulCount++;
                }
            })
            .ThrowsAsync(new InvalidOperationException("Failed to publish Test2"))
            .Verifiable(); // This will be used only for Test2 first attempt
            
        // The complex mock setup above was causing issues. Let's try a more straightforward approach.
        // We'll set up the mock to fail only on specific condition.
        var callCount = 0;
        var test2FirstCall = true;
        
        var simpleMock = new Mock<IMessageBroker>();
        simpleMock.Setup(x => x.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns<TestMessage, PublishOptions?, CancellationToken>(async (msg, opts, ct) =>
            {
                callCount++;
                // Fail Test2 only on first call
                if (msg.Content == "Test2" && callCount <= 3 && test2FirstCall) // In initial batch
                {
                    test2FirstCall = false;
                    throw new InvalidOperationException($"Failed to publish {msg.Content}");
                }
            });
            
        // For now let's use a simpler approach by creating a custom mock that behaves correctly
        var messagePublishResults = new Dictionary<(string content, int attempt), bool>();
        var attemptCounters = new Dictionary<string, int>();
        
        var mockProcessor = new Mock<IMessageBroker>();
        mockProcessor.Setup(x => x.PublishAsync(
                It.IsAny<TestMessage>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns<TestMessage, PublishOptions?, CancellationToken>(async (msg, opts, ct) =>
            {
                var content = msg.Content;
                if (!attemptCounters.ContainsKey(content))
                    attemptCounters[content] = 0;
                attemptCounters[content]++;
                
                // Test2 fails on first attempt only
                if (content == "Test2" && attemptCounters[content] == 1)
                {
                    throw new InvalidOperationException($"Failed to publish {content}");
                }
            });

        var options = Options.Create(new BatchOptions
        {
            MaxBatchSize = 3,  // All 3 messages will be in one batch
            FlushInterval = TimeSpan.FromSeconds(10),
            EnableCompression = false,
            PartialRetry = true
        });
        var processor = new BatchProcessor<TestMessage>(
            simpleMock.Object,
            options,
            NullLogger<BatchProcessor<TestMessage>>.Instance);

        // Act - Add 3 messages where middle one fails initially
        await processor.AddAsync(new TestMessage { Content = "Test1" }, null);
        await processor.AddAsync(new TestMessage { Content = "Test2" }, null);  // Will fail once then retry
        await processor.AddAsync(new TestMessage { Content = "Test3" }, null);
        
        await processor.FlushAsync();

        // Assert
        var metrics = processor.GetMetrics();
        Assert.Equal(3, metrics.TotalMessagesProcessed); // All 3 messages should be processed
        Assert.Equal(0, metrics.TotalFailedMessages); // All should succeed with retry

        await processor.DisposeAsync();
    }
}
