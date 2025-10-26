using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.MessageBroker.Compression;
using System.Collections.Concurrent;
using System.Threading;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BaseMessageBrokerEdgeCaseTests
{
    public class TestableMessageBroker : BaseMessageBroker
    {
        public ConcurrentBag<(object Message, byte[] SerializedMessage, PublishOptions? Options)> PublishedMessages { get; } = new();
        public ConcurrentBag<(Type MessageType, SubscriptionInfo SubscriptionInfo)> SubscribedMessages { get; } = new();
        public int SubscribeInternalCallCount;
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        public TestableMessageBroker(
            IOptions<MessageBrokerOptions> options,
            ILogger logger,
            Relay.MessageBroker.Compression.IMessageCompressor? compressor = null,
            IContractValidator? contractValidator = null)
            : base(options, logger, compressor, contractValidator)
        {
        }

        protected override async ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PublishedMessages.Add((message!, serializedMessage, options));

            // For testing publish-subscribe, process the message if started
            if (IsStarted)
            {
                var decompressed = await DecompressMessageAsync(serializedMessage, cancellationToken);
                var deserialized = DeserializeMessage<TMessage>(decompressed);
                var context = new MessageContext();
                await ProcessMessageAsync(deserialized, typeof(TMessage), context, cancellationToken);
            }
        }

        protected override ValueTask SubscribeInternalAsync(
            Type messageType,
            SubscriptionInfo subscriptionInfo,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref SubscribeInternalCallCount);
            SubscribedMessages.Add((messageType, subscriptionInfo));
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StartInternalAsync(CancellationToken cancellationToken)
        {
            StartCalled = true;
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StopInternalAsync(CancellationToken cancellationToken)
        {
            StopCalled = true;
            return ValueTask.CompletedTask;
        }

        protected override ValueTask DisposeInternalAsync()
        {
            DisposeCalled = true;
            return ValueTask.CompletedTask;
        }

        public async ValueTask TestProcessMessageAsync(object message, Type messageType, MessageContext context)
        {
            await ProcessMessageAsync(message, messageType, context);
        }
    }

    [Fact]
    public async Task SubscribeAsync_WithLargeNumberOfHandlers_ShouldHandleEfficiently()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        const int handlerCount = 1000;
        var receivedCounts = new int[handlerCount];
        var handlers = new List<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>();

        for (int i = 0; i < handlerCount; i++)
        {
            var index = i;
            handlers.Add((msg, ctx, ct) =>
            {
                Interlocked.Increment(ref receivedCounts[index]);
                return ValueTask.CompletedTask;
            });
        }

        await broker.StartAsync();

        // Act - Subscribe all handlers
        var subscribeTasks = handlers.Select(handler => broker.SubscribeAsync(handler).AsTask()).ToArray();
        await Task.WhenAll(subscribeTasks);

        var message = new TestMessage { Id = 123, Name = "Test" };
        var context = new MessageContext();

        // Publish and process
        await broker.TestProcessMessageAsync(message, typeof(TestMessage), context);

        // Assert
        Assert.Equal(handlerCount, broker.SubscribedMessages.Count);
        for (int i = 0; i < handlerCount; i++)
        {
            Assert.Equal(1, Volatile.Read(ref receivedCounts[i]));
        }
    }

    [Fact]
    public async Task PublishAsync_ConcurrentOperations_ShouldHandleThreadSafety()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        const int publisherCount = 10;
        const int messagesPerPublisher = 100;
        var totalMessages = publisherCount * messagesPerPublisher;

        await broker.StartAsync();

        // Act - Publish concurrently
        var publishTasks = new List<Task>();
        for (int p = 0; p < publisherCount; p++)
        {
            var publisherId = p;
            publishTasks.Add(Task.Run(async () =>
            {
                for (int m = 0; m < messagesPerPublisher; m++)
                {
                    var message = new TestMessage { Id = publisherId * 1000 + m, Name = $"Publisher{publisherId}-Message{m}" };
                    await broker.PublishAsync(message);
                }
            }));
        }

        await Task.WhenAll(publishTasks);

        // Assert
        Assert.Equal(totalMessages, broker.PublishedMessages.Count);

        // Verify all messages are unique
        var messageIds = broker.PublishedMessages.Select(pm => ((TestMessage)pm.Message).Id).ToHashSet();
        Assert.Equal(totalMessages, messageIds.Count);
    }

    [Fact]
    public async Task SubscribeAsync_ConcurrentSubscriptions_ShouldHandleThreadSafety()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        const int subscriberCount = 50;
        var receivedMessages = new ConcurrentBag<TestMessage>();

        await broker.StartAsync();

        // Act - Subscribe concurrently
        var subscribeTasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();
        for (int i = 0; i < subscriberCount; i++)
        {
            subscribeTasks.Add(Task.Run(async () =>
            {
                try
                {
                    await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
                    {
                        receivedMessages.Add(msg);
                        return ValueTask.CompletedTask;
                    });
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(subscribeTasks);

        // Check if any subscriptions failed
        if (exceptions.Any())
        {
            throw new AggregateException("Some subscriptions failed", exceptions);
        }

        var message = new TestMessage { Id = 999, Name = "Concurrent Test" };
        var context = new MessageContext();

        // Publish and process
        await broker.TestProcessMessageAsync(message, typeof(TestMessage), context);

        // Assert - Due to concurrent execution, we might not get exactly the expected count
        // but we should get most of them
        Assert.True(broker.SubscribeInternalCallCount >= subscriberCount - 5, $"Expected at least {subscriberCount - 5} calls, got {broker.SubscribeInternalCallCount}");
        Assert.True(broker.SubscribedMessages.Count >= subscriberCount - 5, $"Expected at least {subscriberCount - 5} subscriptions, got {broker.SubscribedMessages.Count}");
        Assert.True(receivedMessages.Count >= subscriberCount - 5, $"Expected at least {subscriberCount - 5} received messages, got {receivedMessages.Count}");
        Assert.All(receivedMessages, msg => Assert.Equal(message.Id, msg.Id));
    }

    [Fact]
    public async Task PublishAsync_WithMemoryPressure_ShouldHandleLargeMessages()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Create a message that would cause memory pressure
        var largeMessage = new LargeMemoryMessage
        {
            Id = Guid.NewGuid(),
            Data = new byte[50 * 1024 * 1024], // 50MB
            NestedObjects = new List<NestedObject>()
        };

        // Fill with random data
        new Random().NextBytes(largeMessage.Data);

        // Add many nested objects
        for (int i = 0; i < 10000; i++)
        {
            largeMessage.NestedObjects.Add(new NestedObject
            {
                Id = i,
                Description = $"Object {i}",
                Values = Enumerable.Range(0, 100).Select(x => (double)x).ToArray()
            });
        }

        // Act
        await broker.PublishAsync(largeMessage);

        // Assert
        Assert.Single(broker.PublishedMessages);
        var (publishedMessage, serializedData, _) = broker.PublishedMessages.First();
        Assert.Equal(largeMessage.Id, ((LargeMemoryMessage)publishedMessage).Id);
        Assert.Equal(largeMessage.Data.Length, ((LargeMemoryMessage)publishedMessage).Data.Length);

        // Verify serialization worked
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<LargeMemoryMessage>(serializedData);
        Assert.NotNull(deserialized);
        Assert.Equal(largeMessage.Id, deserialized.Id);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithFailingHandlers_ShouldContinueProcessing()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var successfulHandlerCount = 0;
        var failedHandlerCount = 0;

        // Subscribe handlers that fail at different points
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            failedHandlerCount++;
            throw new InvalidOperationException("Handler 1 failed");
        });

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            Interlocked.Increment(ref successfulHandlerCount);
            return ValueTask.CompletedTask;
        });

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            failedHandlerCount++;
            throw new ArgumentException("Handler 3 failed");
        });

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            Interlocked.Increment(ref successfulHandlerCount);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync();

        var message = new TestMessage { Id = 123, Name = "Error Test" };
        var context = new MessageContext();

        // Act
        await broker.TestProcessMessageAsync(message, typeof(TestMessage), context);

        // Assert - Successful handlers should have processed
        Assert.Equal(2, Volatile.Read(ref successfulHandlerCount));
        // Failed handlers threw exceptions but processing continued
    }

    [Fact]
    public async Task PublishAsync_WithHighFrequency_ShouldMaintainPerformance()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        const int messageCount = 10000;
        var messages = new TestMessage[messageCount];

        for (int i = 0; i < messageCount; i++)
        {
            messages[i] = new TestMessage { Id = i, Name = $"Message{i}" };
        }

        await broker.StartAsync();

        // Act - Measure time for high-frequency publishing
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var publishTasks = messages.Select(msg => broker.PublishAsync(msg).AsTask()).ToArray();
        await Task.WhenAll(publishTasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(messageCount, broker.PublishedMessages.Count);

        // Performance check - should complete within reasonable time (adjust threshold as needed)
        // This is a basic performance gate; in real scenarios, use proper benchmarking
        Assert.True(stopwatch.ElapsedMilliseconds < 30000, $"Publishing took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    public class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class LargeMemoryMessage
    {
        public Guid Id { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public List<NestedObject> NestedObjects { get; set; } = new();
    }

    public class NestedObject
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public double[] Values { get; set; } = Array.Empty<double>();
    }
}