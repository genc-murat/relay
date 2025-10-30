using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.Batch;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BatchMessageBrokerDecoratorTests : IDisposable
{
    private readonly Mock<IMessageBroker> _innerBrokerMock;
    private readonly Mock<ILogger<BatchMessageBrokerDecorator>> _loggerMock;
    private readonly BatchOptions _options;

    public BatchMessageBrokerDecoratorTests()
    {
        _innerBrokerMock = new Mock<IMessageBroker>();
        _loggerMock = new Mock<ILogger<BatchMessageBrokerDecorator>>();
        _options = new BatchOptions
        {
            Enabled = true,
            MaxBatchSize = 10,
            FlushInterval = TimeSpan.FromSeconds(1),
            EnableCompression = false,
            PartialRetry = true
        };

        // Setup mock to handle any TestMessage publish calls
        _innerBrokerMock.Setup(x => x.PublishAsync(It.IsAny<TestMessage>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        _innerBrokerMock.Setup(x => x.PublishAsync(It.IsAny<byte[]>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
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
            new BatchMessageBrokerDecorator(
                null!,
                Options.Create(_options),
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BatchMessageBrokerDecorator(
                _innerBrokerMock.Object,
                null!,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BatchMessageBrokerDecorator(
                _innerBrokerMock.Object,
                Options.Create(_options),
                null!));
    }

    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowException()
    {
        // Arrange
        var invalidOptions = new BatchOptions { MaxBatchSize = 0 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BatchMessageBrokerDecorator(
                _innerBrokerMock.Object,
                Options.Create(invalidOptions),
                _loggerMock.Object));
    }

    [Fact]
    public async Task PublishAsync_WhenBatchingDisabled_ShouldPublishDirectly()
    {
        // Arrange
        var disabledOptions = new BatchOptions { Enabled = false };
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
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
    }

    [Fact]
    public async Task PublishAsync_WhenBatchingEnabled_ShouldAddToBatch()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Content = "test" };

        // Act
        await decorator.PublishAsync(message);

        // Assert
        var metrics = decorator.GetMetrics<TestMessage>();
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics!.CurrentBatchSize);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
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
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        await decorator.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await decorator.PublishAsync(new TestMessage { Content = "test" }));
    }

    [Fact]
    public async Task SubscribeAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
            Options.Create(_options),
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
    public async Task StartAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
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
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
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
    public void GetMetrics_WhenNoProcessorExists_ShouldReturnNull()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        var metrics = decorator.GetMetrics<TestMessage>();

        // Assert
        Assert.Null(metrics);
    }

    [Fact]
    public async Task GetMetrics_WhenProcessorExists_ShouldReturnMetrics()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        await decorator.PublishAsync(new TestMessage { Content = "test" });

        // Act
        var metrics = decorator.GetMetrics<TestMessage>();

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics!.CurrentBatchSize);
    }

    [Fact]
    public async Task FlushAllAsync_ShouldFlushAllProcessors()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        await decorator.PublishAsync(new TestMessage { Content = "test1" });
        await decorator.PublishAsync(new TestMessage { Content = "test2" });

        var metricsBefore = decorator.GetMetrics<TestMessage>();
        Assert.NotNull(metricsBefore);
        Assert.Equal(2, metricsBefore!.CurrentBatchSize);

        // Act
        await decorator.FlushAllAsync();

        // Assert
        var metricsAfter = decorator.GetMetrics<TestMessage>();
        Assert.NotNull(metricsAfter);
        Assert.Equal(0, metricsAfter!.CurrentBatchSize);
    }

    [Fact]
    public async Task FlushAllAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        await decorator.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await decorator.FlushAllAsync());
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeAllProcessors()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        await decorator.PublishAsync(new TestMessage { Content = "test" });

        // Act
        await decorator.DisposeAsync();

        // Assert - Should not throw, and subsequent operations should fail
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await decorator.PublishAsync(new TestMessage { Content = "test" }));
    }

    [Fact]
    public async Task DisposeAsync_ShouldBeIdempotent()
    {
        // Arrange
        var decorator = new BatchMessageBrokerDecorator(
            _innerBrokerMock.Object,
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