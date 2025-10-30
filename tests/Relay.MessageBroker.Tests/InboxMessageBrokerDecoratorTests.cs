using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.Inbox;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class InboxMessageBrokerDecoratorTests : IDisposable
{
    private readonly Mock<IMessageBroker> _innerBrokerMock;
    private readonly Mock<IInboxStore> _inboxStoreMock;
    private readonly Mock<ILogger<InboxMessageBrokerDecorator>> _loggerMock;
    private readonly InboxOptions _options;

    public InboxMessageBrokerDecoratorTests()
    {
        _innerBrokerMock = new Mock<IMessageBroker>();
        _inboxStoreMock = new Mock<IInboxStore>();
        _loggerMock = new Mock<ILogger<InboxMessageBrokerDecorator>>();
        _options = new InboxOptions
        {
            Enabled = true,
            RetentionPeriod = TimeSpan.FromDays(7),
            CleanupInterval = TimeSpan.FromHours(1),
            ConsumerName = "TestConsumer"
        };
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    [Fact]
    public void Constructor_WithNullInnerBroker_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InboxMessageBrokerDecorator(
                null!,
                _inboxStoreMock.Object,
                Options.Create(_options),
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullInboxStore_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InboxMessageBrokerDecorator(
                _innerBrokerMock.Object,
                null!,
                Options.Create(_options),
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InboxMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _inboxStoreMock.Object,
                null!,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InboxMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _inboxStoreMock.Object,
                Options.Create(_options),
                null!));
    }

    [Fact]
    public async Task PublishAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new InboxMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _inboxStoreMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var options = new PublishOptions { RoutingKey = "test.key" };

        _innerBrokerMock.Setup(x => x.PublishAsync(message, options, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _innerBrokerMock.Verify(x => x.PublishAsync(message, options, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_WhenInboxDisabled_ShouldDelegateDirectly()
    {
        // Arrange
        var disabledOptions = new InboxOptions { Enabled = false };
        var decorator = new InboxMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _inboxStoreMock.Object,
            Options.Create(disabledOptions),
            _loggerMock.Object);

        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = (msg, ctx, ct) => ValueTask.CompletedTask;
        var options = new SubscriptionOptions { QueueName = "test.queue" };

        _innerBrokerMock.Setup(x => x.SubscribeAsync(handler, options, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await decorator.SubscribeAsync(handler, options);

        // Assert
        _innerBrokerMock.Verify(x => x.SubscribeAsync(handler, options, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var decorator = new InboxMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _inboxStoreMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await decorator.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_WhenMessageAlreadyProcessed_ShouldSkipProcessing()
    {
        // Arrange
        var decorator = new InboxMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _inboxStoreMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var context = new MessageContext
        {
            MessageId = "test-message-id",
            Acknowledge = () => ValueTask.CompletedTask
        };

        _inboxStoreMock.Setup(x => x.ExistsAsync("test-message-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<TestMessage, MessageContext, CancellationToken, ValueTask> originalHandler = (msg, ctx, ct) => ValueTask.CompletedTask;
        var options = new SubscriptionOptions { QueueName = "test.queue" };

        _innerBrokerMock.Setup(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), options, CancellationToken.None))
            .Returns(ValueTask.CompletedTask)
            .Callback<Func<TestMessage, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>((handler, opts, ct) =>
            {
                // Simulate calling the wrapped handler
                handler(message, context, ct).AsTask().Wait();
            });

        // Act
        await decorator.SubscribeAsync(originalHandler, options);

        // Assert
        _inboxStoreMock.Verify(x => x.ExistsAsync("test-message-id", It.IsAny<CancellationToken>()), Times.Once);
        _inboxStoreMock.Verify(x => x.StoreAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_WhenMessageNotProcessed_ShouldProcessAndStore()
    {
        // Arrange
        var decorator = new InboxMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _inboxStoreMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var context = new MessageContext
        {
            MessageId = "test-message-id"
        };

        _inboxStoreMock.Setup(x => x.ExistsAsync("test-message-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _inboxStoreMock.Setup(x => x.StoreAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var handlerCalled = false;
        Func<TestMessage, MessageContext, CancellationToken, ValueTask> originalHandler = (msg, ctx, ct) =>
        {
            handlerCalled = true;
            return ValueTask.CompletedTask;
        };

        var options = new SubscriptionOptions { QueueName = "test.queue" };

        _innerBrokerMock.Setup(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), options, CancellationToken.None))
            .Returns(ValueTask.CompletedTask)
            .Callback<Func<TestMessage, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>((handler, opts, ct) =>
            {
                // Simulate calling the wrapped handler
                handler(message, context, ct).AsTask().Wait();
            });

        // Act
        await decorator.SubscribeAsync(originalHandler, options);

        // Assert
        Assert.True(handlerCalled);
        _inboxStoreMock.Verify(x => x.ExistsAsync("test-message-id", It.IsAny<CancellationToken>()), Times.Once);
        _inboxStoreMock.Verify(x => x.StoreAsync(It.Is<InboxMessage>(m =>
            m.MessageId == "test-message-id" &&
            m.MessageType == "TestMessage" &&
            m.ConsumerName == "TestConsumer"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_WhenMessageIdIsNull_ShouldProcessWithoutInboxCheck()
    {
        // Arrange
        var decorator = new InboxMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _inboxStoreMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var context = new MessageContext
        {
            MessageId = null
        };

        var handlerCalled = false;
        Func<TestMessage, MessageContext, CancellationToken, ValueTask> originalHandler = (msg, ctx, ct) =>
        {
            handlerCalled = true;
            return ValueTask.CompletedTask;
        };

        var options = new SubscriptionOptions { QueueName = "test.queue" };

        _innerBrokerMock.Setup(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), options, CancellationToken.None))
            .Returns(ValueTask.CompletedTask)
            .Callback<Func<TestMessage, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>((handler, opts, ct) =>
            {
                // Simulate calling the wrapped handler
                handler(message, context, ct).AsTask().Wait();
            });

        // Act
        await decorator.SubscribeAsync(originalHandler, options);

        // Assert
        Assert.True(handlerCalled);
        _inboxStoreMock.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _inboxStoreMock.Verify(x => x.StoreAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_WhenHandlerThrows_ShouldNotStoreMessage()
    {
        // Arrange
        var decorator = new InboxMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _inboxStoreMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var context = new MessageContext
        {
            MessageId = "test-message-id"
        };

        _inboxStoreMock.Setup(x => x.ExistsAsync("test-message-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<TestMessage, MessageContext, CancellationToken, ValueTask> originalHandler = (msg, ctx, ct) =>
            throw new InvalidOperationException("Handler failed");

        var options = new SubscriptionOptions { QueueName = "test.queue" };

        _innerBrokerMock.Setup(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), options, CancellationToken.None))
            .Returns(ValueTask.CompletedTask)
            .Callback<Func<TestMessage, MessageContext, CancellationToken, ValueTask>, SubscriptionOptions?, CancellationToken>((handler, opts, ct) =>
            {
                // Simulate calling the wrapped handler
                Assert.ThrowsAsync<InvalidOperationException>(() => handler(message, context, ct).AsTask()).Wait();
            });

        // Act & Assert
        await decorator.SubscribeAsync(originalHandler, options);

        _inboxStoreMock.Verify(x => x.StoreAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new InboxMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _inboxStoreMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var cts = new CancellationTokenSource();
        _innerBrokerMock.Setup(x => x.StartAsync(cts.Token)).Returns(ValueTask.CompletedTask);

        // Act
        await decorator.StartAsync(cts.Token);

        // Assert
        _innerBrokerMock.Verify(x => x.StartAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new InboxMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _inboxStoreMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var cts = new CancellationTokenSource();
        _innerBrokerMock.Setup(x => x.StopAsync(cts.Token)).Returns(ValueTask.CompletedTask);

        // Act
        await decorator.StopAsync(cts.Token);

        // Assert
        _innerBrokerMock.Verify(x => x.StopAsync(cts.Token), Times.Once);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}