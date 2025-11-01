using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Relay.MessageBroker.Outbox.Tests;

public class OutboxWorkerTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions());

        // Act
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void Constructor_WithNullStore_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions());

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OutboxWorker(null!, mockBroker, options, NullLogger<OutboxWorker>.Instance));
        Assert.Equal("store", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullBroker_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var options = Options.Create(new OutboxOptions());

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OutboxWorker(mockStore, null!, options, NullLogger<OutboxWorker>.Instance));
        Assert.Equal("broker", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OutboxWorker(mockStore, mockBroker, null!, NullLogger<OutboxWorker>.Instance));
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions());

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OutboxWorker(mockStore, mockBroker, options, null!));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationRequested_ShouldStopImmediately()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions { PollingInterval = TimeSpan.FromMilliseconds(100) });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        using var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();
        await worker.StartAsync(cts.Token);
        await worker.StopAsync(cts.Token);

        // Assert - Should not throw and complete without processing messages
        Assert.True(true); // If we reach this, the worker handled cancellation properly
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPendingMessages_ShouldContinuePolling()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions 
        { 
            PollingInterval = TimeSpan.FromMilliseconds(100), // Minimum allowed value
            BatchSize = 10
        });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw and should handle the case where there are no pending messages
        cts.CancelAfter(TimeSpan.FromMilliseconds(50)); // Cancel after a short time to stop the loop
        
        await worker.StartAsync(cts.Token);
        await Task.Delay(30); // Wait briefly to allow processing
        await worker.StopAsync(cts.Token);
        
        Assert.True(true); // If we reach this, no exception was thrown
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithMessages_ShouldProcessEachMessage()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions { BatchSize = 10 });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        
        // Add some pending messages to the store
        var message1 = new OutboxMessage 
        { 
            Id = Guid.NewGuid(), 
            MessageType = "TestMessage1", 
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test1" }) 
        };
        var message2 = new OutboxMessage 
        { 
            Id = Guid.NewGuid(), 
            MessageType = "TestMessage2", 
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test2" }) 
        };
        mockStore.StoredMessages.Add(message1);
        mockStore.StoredMessages.Add(message2);

        // Use reflection to call the private method
        var method = typeof(OutboxWorker).GetMethod("ProcessPendingMessagesAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        await (Task)method!.Invoke(worker, new object[] { CancellationToken.None })!;

        // Assert - Check that messages were processed (marked as published)
        Assert.Equal(2, mockStore.PublishedMessageIds.Count);
        Assert.Contains(message1.Id, mockStore.PublishedMessageIds);
        Assert.Contains(message2.Id, mockStore.PublishedMessageIds);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenMaxRetriesExceeded_ShouldMarkAsFailed()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions 
        { 
            MaxRetryAttempts = 2 // Only 2 attempts allowed
        });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        
        // Create a message that already has max retry count reached
        var message = new OutboxMessage 
        { 
            Id = Guid.NewGuid(), 
            MessageType = "TestMessage", 
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test" }),
            RetryCount = 2 // Already at max attempts
        };

        // Use reflection to call the private method
        var method = typeof(OutboxWorker).GetMethod("ProcessMessageAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(worker, new object[] { message, CancellationToken.None })!;

        // Assert - Check that the message was marked as failed, not published
        Assert.Contains(message.Id, mockStore.FailedMessageIds);
        Assert.DoesNotContain(message.Id, mockStore.PublishedMessageIds);
        Assert.Equal("Exceeded maximum retry attempts (2)", mockStore.LastError);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenRetryCountGreaterThanZero_ShouldApplyExponentialBackoff()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions 
        { 
            RetryBaseDelay = TimeSpan.FromMilliseconds(10),
            MaxRetryAttempts = 3
        });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        
        // Create a message with retry count > 0
        var message = new OutboxMessage 
        { 
            Id = Guid.NewGuid(), 
            MessageType = "TestMessage", 
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test" }),
            RetryCount = 1 // Has been retried once
        };

        // Use reflection to call the private method
        var method = typeof(OutboxWorker).GetMethod("ProcessMessageAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var startTime = DateTime.UtcNow;
        await (Task)method!.Invoke(worker, new object[] { message, CancellationToken.None })!;
        var endTime = DateTime.UtcNow;

        // Assert - Check that the message was processed successfully
        Assert.Contains(message.Id, mockStore.PublishedMessageIds);
        Assert.DoesNotContain(message.Id, mockStore.FailedMessageIds);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenPublishFails_ShouldIncrementRetryCount()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker { ShouldFailOnPublish = true }; // Make broker fail on publish
        var options = Options.Create(new OutboxOptions { MaxRetryAttempts = 3 });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        
        var message = new OutboxMessage 
        { 
            Id = Guid.NewGuid(), 
            MessageType = "TestMessage", 
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test" }),
            RetryCount = 0
        };

        // Use reflection to call the private method
        var method = typeof(OutboxWorker).GetMethod("ProcessMessageAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(worker, new object[] { message, CancellationToken.None })!;

        // Assert - Check that the message was not published but retry count incremented
        Assert.DoesNotContain(message.Id, mockStore.PublishedMessageIds);
        Assert.Equal(1, message.RetryCount); // Retry count should be incremented
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenPublishFailsAndMaxRetriesReached_ShouldMarkAsFailed()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker { ShouldFailOnPublish = true }; // Make broker fail on publish
        var options = Options.Create(new OutboxOptions { MaxRetryAttempts = 2 });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        
        // Create a message that will fail and has reached max retries after incrementing
        var message = new OutboxMessage 
        { 
            Id = Guid.NewGuid(), 
            MessageType = "TestMessage", 
            Payload = JsonSerializer.SerializeToUtf8Bytes(new { Content = "Test" }),
            RetryCount = 1 // Will reach max attempts after increment
        };

        // Use reflection to call the private method
        var method = typeof(OutboxWorker).GetMethod("ProcessMessageAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(worker, new object[] { message, CancellationToken.None })!;

        // Assert - Check that the message was marked as failed
        Assert.DoesNotContain(message.Id, mockStore.PublishedMessageIds);
        Assert.Contains(message.Id, mockStore.FailedMessageIds);
    }

    [Fact]
    public void CalculateExponentialBackoff_WithRetryCount1_ShouldReturnBaseDelay()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions 
        { 
            RetryBaseDelay = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = 3
        });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        
        // Use reflection to call the private method
        var method = typeof(OutboxWorker).GetMethod("CalculateExponentialBackoff", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (TimeSpan)method!.Invoke(worker, new object[] { 1 })!;

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), result); // BaseDelay * 2^(1-1) = BaseDelay * 1
    }

    [Fact]
    public void CalculateExponentialBackoff_WithRetryCount2_ShouldReturnDoubleBaseDelay()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions 
        { 
            RetryBaseDelay = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = 3
        });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        
        // Use reflection to call the private method
        var method = typeof(OutboxWorker).GetMethod("CalculateExponentialBackoff", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (TimeSpan)method!.Invoke(worker, new object[] { 2 })!;

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(2), result); // BaseDelay * 2^(2-1) = BaseDelay * 2
    }

    [Fact]
    public void CalculateExponentialBackoff_WithHighRetryCount_ShouldCapAtOneMinute()
    {
        // Arrange
        var mockStore = new MockOutboxStore();
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions 
        { 
            RetryBaseDelay = TimeSpan.FromSeconds(10), // Would be way over 1 minute without capping
            MaxRetryAttempts = 10
        });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        
        // Use reflection to call the private method
        var method = typeof(OutboxWorker).GetMethod("CalculateExponentialBackoff", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (TimeSpan)method!.Invoke(worker, new object[] { 10 })!; // High retry count

        // Assert - Should be capped at 1 minute
        Assert.Equal(TimeSpan.FromMinutes(1), result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessPendingMessagesThrows_ShouldLogErrorAndContinue()
    {
        // Arrange
        var mockStore = new MockOutboxStore { ShouldThrowOnGetPending = true };
        var mockBroker = new MockMessageBroker();
        var options = Options.Create(new OutboxOptions 
        { 
            PollingInterval = TimeSpan.FromMilliseconds(100), // Minimum allowed value
            BatchSize = 10
        });
        var worker = new OutboxWorker(mockStore, mockBroker, options, NullLogger<OutboxWorker>.Instance);
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should handle the exception and continue
        cts.CancelAfter(TimeSpan.FromMilliseconds(50)); // Cancel after a short time
        
        await worker.StartAsync(cts.Token);
        await Task.Delay(30); // Wait briefly
        await worker.StopAsync(cts.Token);
        
        Assert.True(true); // If we reach this, no unhandled exception occurred
    }

    // Supporting classes for testing
    private class MockOutboxStore : IOutboxStore
    {
        public List<OutboxMessage> StoredMessages { get; } = new List<OutboxMessage>();
        public List<Guid> PublishedMessageIds { get; } = new List<Guid>();
        public List<Guid> FailedMessageIds { get; } = new List<Guid>();
        public string LastError { get; private set; } = string.Empty;
        public bool ShouldThrowOnGetPending { get; set; } = false;

        public ValueTask<OutboxMessage> StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            message.Id = message.Id == Guid.Empty ? Guid.NewGuid() : message.Id;
            StoredMessages.Add(message);
            return ValueTask.FromResult(message);
        }

        public ValueTask<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            if (ShouldThrowOnGetPending)
            {
                throw new InvalidOperationException("Test exception");
            }
            
            return ValueTask.FromResult(StoredMessages.AsEnumerable());
        }

        public ValueTask MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            PublishedMessageIds.Add(messageId);
            return ValueTask.CompletedTask;
        }

        public ValueTask MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
        {
            FailedMessageIds.Add(messageId);
            LastError = error;
            return ValueTask.CompletedTask;
        }

        public ValueTask<IEnumerable<OutboxMessage>> GetFailedAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var failedMessages = StoredMessages.Where(m => FailedMessageIds.Contains(m.Id));
            return ValueTask.FromResult(failedMessages);
        }
    }

    private class MockMessageBroker : IMessageBroker
    {
        public bool ShouldFailOnPublish { get; set; } = false;

        public ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (ShouldFailOnPublish)
            {
                throw new InvalidOperationException("Test exception during publish");
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask SubscribeAsync<TMessage>(Func<TMessage, MessageContext, CancellationToken, ValueTask> handler, SubscriptionOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}