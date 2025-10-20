using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core;
using Relay.MessageBroker.Compression;
using Relay.Core.Validation.Interfaces;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BaseMessageBrokerSubscriptionTests
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
    }

    [Fact]
    public async Task SubscribeAsync_ShouldStoreSubscriptionInfo()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var message = new TestMessage { Id = 123, Name = "Test" };
        var subscriptionOptions = new SubscriptionOptions
        {
            QueueName = "test-queue",
            RoutingKey = "test.key"
        };

        // Act
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask, subscriptionOptions);

        // Assert
        Assert.Single(broker.SubscribedMessages);
        var (messageType, subscriptionInfo) = broker.SubscribedMessages[0];
        Assert.Equal(typeof(TestMessage), messageType);
        Assert.Equal(subscriptionOptions.QueueName, subscriptionInfo.Options.QueueName);
        Assert.Equal(subscriptionOptions.RoutingKey, subscriptionInfo.Options.RoutingKey);
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_WhenNotStarted_ShouldAutoStart()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        // Assert
        Assert.True(broker.StartCalled);
        Assert.Single(broker.SubscribedMessages);
    }

    [Fact]
    public async Task SubscribeAsync_WithMultipleDifferentMessageTypes_ShouldStoreSeparately()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        await broker.SubscribeAsync<ComplexMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        // Assert
        Assert.Equal(2, broker.SubscribedMessages.Count); // Two different message types
        Assert.Contains(broker.SubscribedMessages, x => x.MessageType == typeof(TestMessage));
        Assert.Contains(broker.SubscribedMessages, x => x.MessageType == typeof(ComplexMessage));
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ComplexMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; }
    }
}