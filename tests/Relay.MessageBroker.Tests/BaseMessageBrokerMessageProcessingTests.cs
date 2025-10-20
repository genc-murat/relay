using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core;
using Relay.MessageBroker.Compression;
using Relay.Core.Validation.Interfaces;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BaseMessageBrokerMessageProcessingTests
{
    public class TestableMessageBroker : BaseMessageBroker
    {
        public List<(object Message, byte[] SerializedMessage, PublishOptions? Options)> PublishedMessages { get; } = new();
        public List<(Type MessageType, SubscriptionInfo SubscriptionInfo)> SubscribedMessages { get; } = new();
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

        protected override ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            PublishedMessages.Add((message!, serializedMessage, options));
            return ValueTask.CompletedTask;
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

    private class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}