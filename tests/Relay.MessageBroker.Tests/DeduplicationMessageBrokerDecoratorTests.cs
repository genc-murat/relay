using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.Deduplication;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class DeduplicationMessageBrokerDecoratorTests
{
    private readonly Mock<IMessageBroker> _innerBrokerMock;
    private readonly Mock<IDeduplicationCache> _cacheMock;
    private readonly Mock<ILogger<DeduplicationMessageBrokerDecorator>> _loggerMock;
    private readonly DeduplicationOptions _options;

    public DeduplicationMessageBrokerDecoratorTests()
    {
        _innerBrokerMock = new Mock<IMessageBroker>();
        _cacheMock = new Mock<IDeduplicationCache>();
        _loggerMock = new Mock<ILogger<DeduplicationMessageBrokerDecorator>>();
        _options = new DeduplicationOptions { Enabled = true, Window = TimeSpan.FromMinutes(5) };
    }

    [Fact]
    public void Constructor_WithNullInnerBroker_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationMessageBrokerDecorator(
                null!,
                _cacheMock.Object,
                Options.Create(_options),
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationMessageBrokerDecorator(
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
            new DeduplicationMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _cacheMock.Object,
                null!,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _cacheMock.Object,
                Options.Create(_options),
                null!));
    }

    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowException()
    {
        // Arrange
        var invalidOptions = new DeduplicationOptions { Window = TimeSpan.FromHours(25) };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DeduplicationMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _cacheMock.Object,
                Options.Create(invalidOptions),
                _loggerMock.Object));
    }

    [Fact]
    public async Task PublishAsync_WhenDeduplicationDisabled_ShouldPublishDirectly()
    {
        // Arrange
        var disabledOptions = new DeduplicationOptions { Enabled = false };
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(disabledOptions),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act
        await decorator.PublishAsync(message);

        // Assert
        _innerBrokerMock.Verify(b => b.PublishAsync(message, null, default), Times.Once);
        _cacheMock.Verify(c => c.IsDuplicateAsync(It.IsAny<string>(), default), Times.Never);
        _cacheMock.Verify(c => c.AddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), default), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WhenMessageIsNotDuplicate_ShouldPublishAndAddToCache()
    {
        // Arrange
        _cacheMock.Setup(c => c.IsDuplicateAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act
        await decorator.PublishAsync(message);

        // Assert
        _innerBrokerMock.Verify(b => b.PublishAsync(message, null, default), Times.Once);
        _cacheMock.Verify(c => c.IsDuplicateAsync(It.IsAny<string>(), default), Times.Once);
        _cacheMock.Verify(c => c.AddAsync(It.IsAny<string>(), _options.Window, default), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenMessageIsDuplicate_ShouldDiscardMessage()
    {
        // Arrange
        _cacheMock.Setup(c => c.IsDuplicateAsync(It.IsAny<string>(), default)).ReturnsAsync(true);
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act
        await decorator.PublishAsync(message);

        // Assert
        _innerBrokerMock.Verify(b => b.PublishAsync(It.IsAny<TestMessage>(), null, default), Times.Never);
        _cacheMock.Verify(c => c.IsDuplicateAsync(It.IsAny<string>(), default), Times.Once);
        _cacheMock.Verify(c => c.AddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), default), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WhenPublishFails_ShouldReThrowException()
    {
        // Arrange
        _cacheMock.Setup(c => c.IsDuplicateAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _innerBrokerMock.Setup(b => b.PublishAsync(It.IsAny<TestMessage>(), null, default))
            .ThrowsAsync(new Exception("Publish failed"));
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await decorator.PublishAsync(message));
    }

    [Fact]
    public async Task PublishAsync_WithContentHashStrategy_ShouldGenerateContentHash()
    {
        // Arrange
        var contentHashOptions = new DeduplicationOptions
        {
            Enabled = true,
            Strategy = DeduplicationStrategy.ContentHash,
            Window = TimeSpan.FromMinutes(5)
        };
        _cacheMock.Setup(c => c.IsDuplicateAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(contentHashOptions),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act
        await decorator.PublishAsync(message);

        // Assert
        _cacheMock.Verify(c => c.IsDuplicateAsync(It.IsAny<string>(), default), Times.Once);
        _cacheMock.Verify(c => c.AddAsync(It.IsAny<string>(), contentHashOptions.Window, default), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithMessageIdStrategy_ShouldUseMessageIdHeader()
    {
        // Arrange
        var messageIdOptions = new DeduplicationOptions
        {
            Enabled = true,
            Strategy = DeduplicationStrategy.MessageId,
            Window = TimeSpan.FromMinutes(5)
        };
        _cacheMock.Setup(c => c.IsDuplicateAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(messageIdOptions),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object?> { ["MessageId"] = "test-id" }
        };

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _cacheMock.Verify(c => c.IsDuplicateAsync("test-id", default), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithMessageIdStrategy_MissingHeader_ShouldThrowException()
    {
        // Arrange
        var messageIdOptions = new DeduplicationOptions
        {
            Enabled = true,
            Strategy = DeduplicationStrategy.MessageId,
            Window = TimeSpan.FromMinutes(5)
        };
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(messageIdOptions),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await decorator.PublishAsync(message));
    }

    [Fact]
    public async Task PublishAsync_WithCustomStrategy_ShouldUseCustomHashFunction()
    {
        // Arrange
        var customOptions = new DeduplicationOptions
        {
            Enabled = true,
            Strategy = DeduplicationStrategy.Custom,
            Window = TimeSpan.FromMinutes(5),
            CustomHashFunction = data => "custom-hash"
        };
        _cacheMock.Setup(c => c.IsDuplicateAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(customOptions),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act
        await decorator.PublishAsync(message);

        // Assert
        _cacheMock.Verify(c => c.IsDuplicateAsync("custom-hash", default), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        Func<TestMessage, MessageContext, CancellationToken, ValueTask> handler = (msg, ctx, ct) => default;

        // Act
        await decorator.SubscribeAsync(handler);

        // Assert
        _innerBrokerMock.Verify(b => b.SubscribeAsync(handler, null, default), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        await decorator.StartAsync();

        // Assert
        _innerBrokerMock.Verify(b => b.StartAsync(default), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        await decorator.StopAsync();

        // Assert
        _innerBrokerMock.Verify(b => b.StopAsync(default), Times.Once);
    }

    [Fact]
    public void GetMetrics_ShouldReturnCacheMetrics()
    {
        // Arrange
        var expectedMetrics = new DeduplicationMetrics { CurrentCacheSize = 10 };
        _cacheMock.Setup(c => c.GetMetrics()).Returns(expectedMetrics);
        var decorator = new DeduplicationMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _cacheMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        var metrics = decorator.GetMetrics();

        // Assert
        Assert.Equal(expectedMetrics, metrics);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}