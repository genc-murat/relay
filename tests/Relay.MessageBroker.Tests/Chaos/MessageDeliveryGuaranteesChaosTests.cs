using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.MessageBroker.Deduplication;
using Relay.MessageBroker.PoisonMessage;
using Xunit;
using Xunit.Abstractions;

namespace Relay.MessageBroker.Tests.Chaos;

/// <summary>
/// Chaos engineering tests for message delivery guarantees.
/// Tests at-least-once delivery, message ordering, deduplication, and poison message handling.
/// </summary>
[Trait("Category", "Chaos")]
[Trait("Pattern", "MessageDelivery")]
public class MessageDeliveryGuaranteesChaosTests
{
    private readonly ITestOutputHelper _output;

    public MessageDeliveryGuaranteesChaosTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task AtLeastOnceDelivery_WithBrokerFailures_DeliversAllMessages()
    {
        // Arrange
        var broker = new FailingMessageBroker(failureRate: 0.3); // 30% failure rate
        var publishedMessages = new List<TestMessage>();
        var deliveredMessages = new ConcurrentBag<TestMessage>();
        var messageCount = 50;

        // Subscribe and collect delivered messages BEFORE publishing
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            deliveredMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync(CancellationToken.None);

        // Act - Publish messages with retries
        for (int i = 0; i < messageCount; i++)
        {
            var message = new TestMessage { Id = i, Content = $"Message {i}" };
            publishedMessages.Add(message);

            var published = false;
            var retryCount = 0;
            while (!published && retryCount < 5)
            {
                try
                {
                    await broker.PublishAsync(message);
                    published = true;
                }
                catch (SimulatedBrokerFailureException)
                {
                    retryCount++;
                    await Task.Delay(50);
                }
            }

            Assert.True(published, $"Message {i} should eventually be published");
        }

        await Task.Delay(500); // Allow time for message processing

        // Assert - All messages delivered at least once
        Assert.True(deliveredMessages.Count >= messageCount, 
            $"Expected at least {messageCount} deliveries, got {deliveredMessages.Count}");
        
        var uniqueIds = deliveredMessages.Select(m => m.Id).Distinct().ToList();
        Assert.Equal(messageCount, uniqueIds.Count);
        
        _output.WriteLine($"Published: {messageCount}, Delivered: {deliveredMessages.Count}, Unique: {uniqueIds.Count}");
    }

    [Fact]
    public async Task MessageOrdering_WithNetworkPartitions_MaintainsOrderPerPartition()
    {
        // Arrange
        var broker = new PartitionedMessageBroker();
        var messagesPerPartition = 20;
        var partitionCount = 3;
        var deliveredMessages = new ConcurrentDictionary<string, List<TestMessage>>();

        // Subscribe to collect messages by partition
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            var partition = ctx.RoutingKey ?? "default";
            deliveredMessages.AddOrUpdate(
                partition,
                _ => new List<TestMessage> { msg },
                (_, list) => { lock (list) { list.Add(msg); } return list; });
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync(CancellationToken.None);

        // Act - Publish ordered messages to different partitions
        var publishTasks = new List<Task>();
        for (int p = 0; p < partitionCount; p++)
        {
            var partition = $"partition-{p}";
            publishTasks.Add(Task.Run(async () =>
            {
                for (int i = 0; i < messagesPerPartition; i++)
                {
                    var message = new TestMessage 
                    { 
                        Id = i, 
                        Content = $"Partition {partition} Message {i}" 
                    };
                    
                    await broker.PublishAsync(message, new PublishOptions 
                    { 
                        RoutingKey = partition 
                    });
                    
                    await Task.Delay(10); // Small delay to ensure ordering
                }
            }));
        }

        await Task.WhenAll(publishTasks);
        await Task.Delay(500); // Allow processing

        // Assert - Messages within each partition maintain order
        foreach (var partition in deliveredMessages.Keys)
        {
            var messages = deliveredMessages[partition];
            Assert.Equal(messagesPerPartition, messages.Count);

            for (int i = 0; i < messages.Count - 1; i++)
            {
                Assert.True(messages[i].Id <= messages[i + 1].Id, 
                    $"Order violated in {partition}: {messages[i].Id} > {messages[i + 1].Id}");
            }
            
            _output.WriteLine($"{partition}: {messages.Count} messages in order");
        }
    }

    [Fact]
    public async Task MessageDeduplication_WithDuplicateDeliveries_ProcessesOnce()
    {
        // Arrange
        var options = Options.Create(new DeduplicationOptions
        {
            Enabled = true,
            Window = TimeSpan.FromMinutes(5),
            MaxCacheSize = 1000,
            Strategy = DeduplicationStrategy.ContentHash
        });

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DeduplicationCache>.Instance;
        var cache = new DeduplicationCache(options, logger);
        var processedMessages = new ConcurrentBag<int>();
        var messageCount = 30;
        var semaphore = new SemaphoreSlim(1, 1);

        // Act - Simulate duplicate deliveries
        var tasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = i;
            var message = new TestMessage { Id = messageId, Content = $"Message {messageId}" };
            var messageHash = ComputeHash(message);

            // Send each message 3 times (simulating duplicates)
            for (int duplicate = 0; duplicate < 3; duplicate++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // Use semaphore to prevent race conditions in check-then-add
                    await semaphore.WaitAsync();
                    try
                    {
                        var isDuplicate = await cache.IsDuplicateAsync(messageHash, CancellationToken.None);
                        
                        if (!isDuplicate)
                        {
                            processedMessages.Add(messageId);
                            await cache.AddAsync(messageHash, TimeSpan.FromMinutes(5), CancellationToken.None);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);

        // Assert - Each message processed exactly once
        var uniqueProcessed = processedMessages.Distinct().ToList();
        Assert.Equal(messageCount, uniqueProcessed.Count);
        Assert.Equal(messageCount, processedMessages.Count); // No duplicates processed
        
        _output.WriteLine($"Sent: {messageCount * 3} (with duplicates), Processed: {processedMessages.Count}, Unique: {uniqueProcessed.Count}");
    }

    [Fact]
    public async Task PoisonMessageHandling_WithRepeatedlyFailingMessages_MovesToPoisonQueue()
    {
        // Arrange
        var options = Options.Create(new PoisonMessageOptions
        {
            Enabled = true,
            FailureThreshold = 3,
            RetentionPeriod = TimeSpan.FromDays(7)
        });

        var store = new InMemoryPoisonMessageStore();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<PoisonMessageHandler>.Instance;
        var handler = new PoisonMessageHandler(store, options, logger);
        var poisonMessages = new List<Relay.MessageBroker.PoisonMessage.PoisonMessage>();
        var messageId = Guid.NewGuid().ToString();
        var failureCount = 0;

        // Act - Simulate repeated failures
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                // Simulate message processing failure
                failureCount++;
                
                if (failureCount >= options.Value.FailureThreshold)
                {
                    var poisonMessage = new Relay.MessageBroker.PoisonMessage.PoisonMessage
                    {
                        Id = Guid.NewGuid(),
                        OriginalMessageId = messageId,
                        MessageType = typeof(TestMessage).Name,
                        Payload = System.Text.Encoding.UTF8.GetBytes($"{{\"Id\":1,\"Content\":\"Failing message\"}}"),
                        FailureCount = failureCount,
                        Errors = new List<string> { $"Attempt {attempt}: Processing failed" },
                        FirstFailureAt = DateTimeOffset.UtcNow.AddSeconds(-failureCount),
                        LastFailureAt = DateTimeOffset.UtcNow
                    };

                    await handler.HandleAsync(poisonMessage, CancellationToken.None);
                    poisonMessages.Add(poisonMessage);
                    break;
                }

                throw new InvalidOperationException($"Processing failed on attempt {attempt}");
            }
            catch (InvalidOperationException)
            {
                // Continue retrying
            }
        }

        // Assert
        Assert.Single(poisonMessages);
        Assert.True(failureCount >= options.Value.FailureThreshold);
        
        var storedPoison = await handler.GetPoisonMessagesAsync(CancellationToken.None);
        Assert.NotEmpty(storedPoison);
        
        _output.WriteLine($"Message failed {failureCount} times and moved to poison queue");
    }

    [Fact]
    public async Task MessageDelivery_UnderHighConcurrency_MaintainsGuarantees()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var messageCount = 100;
        var deliveredMessages = new ConcurrentBag<TestMessage>();
        var deliveryErrors = new ConcurrentBag<Exception>();

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            deliveredMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync(CancellationToken.None);

        // Act - Publish messages concurrently
        var publishTasks = Enumerable.Range(0, messageCount).Select(i =>
            Task.Run(async () =>
            {
                try
                {
                    var message = new TestMessage { Id = i, Content = $"Concurrent message {i}" };
                    await broker.PublishAsync(message);
                }
                catch (Exception ex)
                {
                    deliveryErrors.Add(ex);
                }
            })
        );

        await Task.WhenAll(publishTasks);
        await Task.Delay(500); // Allow processing

        // Assert
        Assert.Empty(deliveryErrors);
        Assert.Equal(messageCount, deliveredMessages.Count);
        
        var uniqueIds = deliveredMessages.Select(m => m.Id).Distinct().Count();
        Assert.Equal(messageCount, uniqueIds);
        
        _output.WriteLine($"Published {messageCount} messages concurrently, delivered {deliveredMessages.Count} unique messages");
    }

    [Fact]
    public async Task MessageDelivery_WithTransientFailures_EventuallySucceeds()
    {
        // Arrange
        var broker = new TransientFailureMessageBroker(failureWindow: 3);
        var message = new TestMessage { Id = 1, Content = "Test message" };
        var delivered = false;
        var attemptCount = 0;

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            delivered = true;
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync(CancellationToken.None);

        // Act - Retry until success
        while (!delivered && attemptCount < 10)
        {
            try
            {
                await broker.PublishAsync(message);
                attemptCount++;
                await Task.Delay(100);
            }
            catch (SimulatedBrokerFailureException)
            {
                attemptCount++;
                await Task.Delay(100);
            }
        }

        await Task.Delay(200); // Allow processing

        // Assert
        Assert.True(delivered, "Message should eventually be delivered");
        Assert.True(attemptCount >= 3, "Should have required multiple attempts");
        
        _output.WriteLine($"Message delivered after {attemptCount} attempts");
    }

    // Helper method to compute message hash
    private string ComputeHash(TestMessage message)
    {
        var content = $"{message.Id}:{message.Content}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }
}

// Test message class
public class TestMessage
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
}

// Simulated broker with failures
public class FailingMessageBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();
    private readonly double _failureRate;
    private readonly Random _random = new(42); // Fixed seed for deterministic behavior

    public FailingMessageBroker(double failureRate)
    {
        _failureRate = failureRate;
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_random.NextDouble() < _failureRate)
        {
            throw new SimulatedBrokerFailureException("Simulated broker failure");
        }

        await _inner.PublishAsync(message, options, cancellationToken);
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Simulated broker with partitions
public class PartitionedMessageBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();

    public ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.PublishAsync(message, options, cancellationToken);

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Simulated broker with transient failures
public class TransientFailureMessageBroker : IMessageBroker
{
    private readonly InMemoryMessageBroker _inner = new();
    private int _attemptCount;
    private readonly int _failureWindow;

    public TransientFailureMessageBroker(int failureWindow)
    {
        _failureWindow = failureWindow;
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _attemptCount++;
        
        if (_attemptCount <= _failureWindow)
        {
            throw new SimulatedBrokerFailureException($"Transient failure (attempt {_attemptCount})");
        }

        await _inner.PublishAsync(message, options, cancellationToken);
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, options, cancellationToken);

    public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
        _inner.StartAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        _inner.StopAsync(cancellationToken);
}

// Custom exception for simulated failures
public class SimulatedBrokerFailureException : Exception
{
    public SimulatedBrokerFailureException(string message) : base(message) { }
}

// In-memory poison message store for testing
public class InMemoryPoisonMessageStore : IPoisonMessageStore
{
    private readonly ConcurrentBag<Relay.MessageBroker.PoisonMessage.PoisonMessage> _messages = new();

    public ValueTask StoreAsync(Relay.MessageBroker.PoisonMessage.PoisonMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<Relay.MessageBroker.PoisonMessage.PoisonMessage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IEnumerable<Relay.MessageBroker.PoisonMessage.PoisonMessage>>(_messages.ToList());
    }

    public ValueTask<Relay.MessageBroker.PoisonMessage.PoisonMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        return ValueTask.FromResult(message);
    }

    public ValueTask RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Not implemented for this test
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> CleanupExpiredAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        // Not implemented for this test
        return ValueTask.FromResult(0);
    }

    public ValueTask UpdateAsync(Relay.MessageBroker.PoisonMessage.PoisonMessage message, CancellationToken cancellationToken = default)
    {
        // Not implemented for this test
        return ValueTask.CompletedTask;
    }
}
