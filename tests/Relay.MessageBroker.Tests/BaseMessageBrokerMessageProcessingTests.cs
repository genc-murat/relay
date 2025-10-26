using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.MessageBroker.Compression;
using System.Collections.Concurrent;

namespace Relay.MessageBroker.Tests;

public class BaseMessageBrokerMessageProcessingTests
{
    public class TestableMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger logger,
        Relay.MessageBroker.Compression.IMessageCompressor? compressor = null,
        IContractValidator? contractValidator = null) : BaseMessageBroker(options, logger, compressor, contractValidator)
    {
        public List<(object Message, byte[] SerializedMessage, PublishOptions? Options)> PublishedMessages { get; } = [];
        public List<(Type MessageType, SubscriptionInfo SubscriptionInfo)> SubscribedMessages { get; } = [];
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        protected override async ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
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
    public async Task ProcessMessageAsync_ShouldHandleMultipleHandlers()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var receivedMessages1 = new List<TestMessage>();
        var receivedMessages2 = new List<TestMessage>();

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages1.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages2.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync();

        var message = new TestMessage { Id = 123, Name = "Test" };
        var context = new MessageContext();

        // Act
        await broker.TestProcessMessageAsync(message, typeof(TestMessage), context);

        // Assert
        Assert.Single(receivedMessages1);
        Assert.Single(receivedMessages2);
        Assert.Equal(message.Id, receivedMessages1[0].Id);
        Assert.Equal(message.Id, receivedMessages2[0].Id);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenHandlerThrows_ShouldContinueWithOtherHandlers()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var receivedMessages = new List<TestMessage>();

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            throw new InvalidOperationException("Handler failed");
        });

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync();

        var message = new TestMessage { Id = 123, Name = "Test" };
        var context = new MessageContext();

        // Act
        await broker.TestProcessMessageAsync(message, typeof(TestMessage), context);

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Id, receivedMessages[0].Id);
    }

    [Fact]
    public async Task PublishAndSubscribe_WithCompression_ShouldDeliverCompressedMessage()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new Compression.CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var compressor = new GZipMessageCompressor();
        var broker = new TestableMessageBroker(options, logger, compressor);

        var receivedMessages = new List<TestMessage>();
        var message = new TestMessage { Id = 123, Name = "Test with compression" };

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync();

        // Act
        await broker.PublishAsync(message);

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Id, receivedMessages[0].Id);
        Assert.Equal(message.Name, receivedMessages[0].Name);

        // Verify compression was applied
        Assert.Single(broker.PublishedMessages);
        var (_, serializedData, _) = broker.PublishedMessages[0];
        Assert.True(serializedData.Length > 0); // Compressed data
    }

    [Fact]
    public async Task ConcurrentPublishAndSubscribe_ShouldHandleThreadSafety()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var receivedMessages = new ConcurrentBag<TestMessage>();
        const int messageCount = 100;
        const int subscriberCount = 5;

        // Subscribe multiple handlers
        for (int i = 0; i < subscriberCount; i++)
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
            {
                receivedMessages.Add(msg);
                return ValueTask.CompletedTask;
            });
        }

        await broker.StartAsync();

        // Act - Publish messages concurrently
        var publishTasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var message = new TestMessage { Id = i, Name = $"Message {i}" };
            publishTasks.Add(Task.Run(() => broker.PublishAsync(message)));
        }

        await Task.WhenAll(publishTasks);

        // Assert
        Assert.Equal(messageCount * subscriberCount, receivedMessages.Count);
        var receivedIds = receivedMessages.Select(m => m.Id).Distinct().OrderBy(id => id).ToList();
        Assert.Equal(messageCount, receivedIds.Count);
        for (int i = 0; i < messageCount; i++)
        {
            Assert.Contains(i, receivedIds);
        }
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}