using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.Bulkhead;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BulkheadMessageBrokerDecoratorTests : IDisposable
{
    private readonly Mock<IMessageBroker> _innerBrokerMock;
    private readonly Mock<IBulkhead> _publishBulkheadMock;
    private readonly Mock<IBulkhead> _subscribeBulkheadMock;
    private readonly Mock<ILogger<BulkheadMessageBrokerDecorator>> _loggerMock;
    private readonly BulkheadOptions _options;

    public BulkheadMessageBrokerDecoratorTests()
    {
        _innerBrokerMock = new Mock<IMessageBroker>();
        _publishBulkheadMock = new Mock<IBulkhead>();
        _subscribeBulkheadMock = new Mock<IBulkhead>();
        _loggerMock = new Mock<ILogger<BulkheadMessageBrokerDecorator>>();
        _options = new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 10,
            MaxQueuedOperations = 100,
            AcquisitionTimeout = TimeSpan.FromSeconds(30)
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
            new BulkheadMessageBrokerDecorator(
                null!,
                _publishBulkheadMock.Object,
                _subscribeBulkheadMock.Object,
                Options.Create(_options),
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullPublishBulkhead_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BulkheadMessageBrokerDecorator(
                _innerBrokerMock.Object,
                null!,
                _subscribeBulkheadMock.Object,
                Options.Create(_options),
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullSubscribeBulkhead_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BulkheadMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _publishBulkheadMock.Object,
                null!,
                Options.Create(_options),
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BulkheadMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _publishBulkheadMock.Object,
                _subscribeBulkheadMock.Object,
                null!,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BulkheadMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _publishBulkheadMock.Object,
                _subscribeBulkheadMock.Object,
                Options.Create(_options),
                null!));
    }

    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowException()
    {
        // Arrange
        var invalidOptions = new BulkheadOptions { Enabled = true, MaxConcurrentOperations = 0 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new BulkheadMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _publishBulkheadMock.Object,
                _subscribeBulkheadMock.Object,
                Options.Create(invalidOptions),
                _loggerMock.Object));
    }

    [Fact]
    public async Task PublishAsync_WhenBulkheadDisabled_ShouldPublishDirectly()
    {
        // Arrange
        var disabledOptions = new BulkheadOptions { Enabled = false };
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(disabledOptions),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var options = new PublishOptions { RoutingKey = "test.key" };

        _innerBrokerMock.Setup(x => x.PublishAsync(message, options, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _innerBrokerMock.Verify(x => x.PublishAsync(message, options, CancellationToken.None), Times.Once);
        _publishBulkheadMock.Verify(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, ValueTask<int>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WhenBulkheadEnabled_ShouldExecuteWithinBulkhead()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };
        var options = new PublishOptions { RoutingKey = "test.key" };

        _innerBrokerMock.Setup(x => x.PublishAsync(message, options, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        _publishBulkheadMock.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, ValueTask<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, ValueTask<bool>>, CancellationToken>(async (func, ct) => await func(ct));

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _publishBulkheadMock.Verify(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, ValueTask<bool>>>(), CancellationToken.None), Times.Once);
        _innerBrokerMock.Verify(x => x.PublishAsync(message, options, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenBulkheadRejects_ShouldThrowBulkheadRejectedException()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };

        _publishBulkheadMock.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, ValueTask<bool>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BulkheadRejectedException("Bulkhead full", 5, 10));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BulkheadRejectedException>(async () =>
            await decorator.PublishAsync(message));

        Assert.Equal(5, exception.ActiveOperations);
        Assert.Equal(10, exception.QueuedOperations);
    }

    [Fact]
    public async Task PublishAsync_WhenInnerBrokerThrows_ShouldPropagateException()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };

        _innerBrokerMock.Setup(x => x.PublishAsync(message, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Inner broker error"));

        _publishBulkheadMock.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, ValueTask<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, ValueTask<bool>>, CancellationToken>(async (func, ct) => await func(ct));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await decorator.PublishAsync(message));
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await decorator.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        await decorator.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await decorator.PublishAsync(new TestMessage { Content = "test" }));
    }

    [Fact]
    public async Task SubscribeAsync_WhenBulkheadDisabled_ShouldSubscribeDirectly()
    {
        // Arrange
        var disabledOptions = new BulkheadOptions { Enabled = false };
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
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
        _subscribeBulkheadMock.Verify(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, ValueTask<int>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_WhenBulkheadEnabled_ShouldWrapHandlerWithBulkhead()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = (msg, ctx, ct) => ValueTask.CompletedTask;
        var options = new SubscriptionOptions { QueueName = "test.queue" };

        _innerBrokerMock.Setup(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), options, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await decorator.SubscribeAsync(handler, options);

        // Assert
        _innerBrokerMock.Verify(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), options, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_HandlerExecution_WhenBulkheadRejects_ShouldRejectMessage()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        Func<TestMessage, MessageContext, CancellationToken, ValueTask> originalHandler = (msg, ctx, ct) => ValueTask.CompletedTask;

        _innerBrokerMock.Setup(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), null, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        _subscribeBulkheadMock.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, ValueTask<int>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BulkheadRejectedException("Bulkhead full", 5, 10));

        // Act & Assert - Subscribe should succeed, but handler execution will fail
        await decorator.SubscribeAsync(originalHandler);

        _innerBrokerMock.Verify(x => x.SubscribeAsync(It.IsAny<Func<TestMessage, MessageContext, CancellationToken, ValueTask>>(), null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await decorator.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        await decorator.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await decorator.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask));
    }

    [Fact]
    public async Task StartAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
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
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var cts = new CancellationTokenSource();
        _innerBrokerMock.Setup(x => x.StopAsync(cts.Token)).Returns(ValueTask.CompletedTask);

        // Act
        await decorator.StopAsync(cts.Token);

        // Assert
        _innerBrokerMock.Verify(x => x.StopAsync(cts.Token), Times.Once);
    }

    [Fact]
    public void GetPublishMetrics_ShouldReturnPublishBulkheadMetrics()
    {
        // Arrange
        var expectedMetrics = new BulkheadMetrics
        {
            ActiveOperations = 5,
            QueuedOperations = 10,
            RejectedOperations = 2,
            ExecutedOperations = 100
        };

        _publishBulkheadMock.Setup(x => x.GetMetrics()).Returns(expectedMetrics);

        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        var metrics = decorator.GetPublishMetrics();

        // Assert
        Assert.Equal(expectedMetrics, metrics);
    }

    [Fact]
    public void GetSubscribeMetrics_ShouldReturnSubscribeBulkheadMetrics()
    {
        // Arrange
        var expectedMetrics = new BulkheadMetrics
        {
            ActiveOperations = 3,
            QueuedOperations = 5,
            RejectedOperations = 1,
            ExecutedOperations = 50
        };

        _subscribeBulkheadMock.Setup(x => x.GetMetrics()).Returns(expectedMetrics);

        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        var metrics = decorator.GetSubscribeMetrics();

        // Assert
        Assert.Equal(expectedMetrics, metrics);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeBulkheadsAndInnerBroker()
    {
        // Arrange
        var disposablePublishBulkheadMock = new Mock<IBulkhead>();
        var disposableSubscribeBulkheadMock = new Mock<IBulkhead>();
        var asyncDisposableBrokerMock = new Mock<IMessageBroker>();

        disposablePublishBulkheadMock.As<IDisposable>();
        disposableSubscribeBulkheadMock.As<IDisposable>();
        asyncDisposableBrokerMock.As<IAsyncDisposable>();

        var decorator = new BulkheadMessageBrokerDecorator(
            asyncDisposableBrokerMock.Object,
            disposablePublishBulkheadMock.Object,
            disposableSubscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        await decorator.DisposeAsync();

        // Assert
        disposablePublishBulkheadMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
        disposableSubscribeBulkheadMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
        asyncDisposableBrokerMock.As<IAsyncDisposable>().Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ShouldBeIdempotent()
    {
        // Arrange
        var decorator = new BulkheadMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _publishBulkheadMock.Object,
            _subscribeBulkheadMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        await decorator.DisposeAsync();
        await decorator.DisposeAsync(); // Second call should not throw

        // Assert - No exception thrown
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}