using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.RateLimit;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RateLimitMessageBrokerDecoratorTests : IDisposable
{
    private readonly Mock<IMessageBroker> _innerBrokerMock;
    private readonly Mock<IRateLimiter> _rateLimiterMock;
    private readonly Mock<ILogger<RateLimitMessageBrokerDecorator>> _loggerMock;
    private readonly RateLimitOptions _options;

    public RateLimitMessageBrokerDecoratorTests()
    {
        _innerBrokerMock = new Mock<IMessageBroker>();
        _rateLimiterMock = new Mock<IRateLimiter>();
        _loggerMock = new Mock<ILogger<RateLimitMessageBrokerDecorator>>();
        _options = new RateLimitOptions
        {
            Enabled = true,
            RequestsPerSecond = 100,
            Strategy = RateLimitStrategy.TokenBucket
        };
    }

    [Fact]
    public void Constructor_WithNullInnerBroker_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RateLimitMessageBrokerDecorator(
                null!,
                _rateLimiterMock.Object,
                Options.Create(_options),
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullRateLimiter_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RateLimitMessageBrokerDecorator(
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
            new RateLimitMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _rateLimiterMock.Object,
                null!,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RateLimitMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _rateLimiterMock.Object,
                Options.Create(_options),
                null!));
    }

    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowException()
    {
        // Arrange
        var invalidOptions = new RateLimitOptions { Enabled = true, RequestsPerSecond = 0 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new RateLimitMessageBrokerDecorator(
                _innerBrokerMock.Object,
                _rateLimiterMock.Object,
                Options.Create(invalidOptions),
                _loggerMock.Object));
    }

    [Fact]
    public async Task PublishAsync_WhenRateLimitingDisabled_ShouldPublishDirectly()
    {
        // Arrange
        var disabledOptions = new RateLimitOptions { Enabled = false };
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _rateLimiterMock.Object,
            Options.Create(disabledOptions),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act
        await decorator.PublishAsync(message);

        // Assert
        _innerBrokerMock.Verify(b => b.PublishAsync(message, null, default), Times.Once);
        _rateLimiterMock.Verify(r => r.CheckAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WhenRequestAllowed_ShouldPublishAndAddHeaders()
    {
        // Arrange
        _rateLimiterMock.Setup(r => r.CheckAsync(It.IsAny<string>(), default))
            .ReturnsAsync(RateLimitResult.Allow(99, DateTimeOffset.UtcNow.AddSeconds(1)));
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _rateLimiterMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };
        var options = new PublishOptions { Headers = new Dictionary<string, object?>() };

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _innerBrokerMock.Verify(b => b.PublishAsync(message, options, default), Times.Once);
        _rateLimiterMock.Verify(r => r.CheckAsync("global", default), Times.Once);
        Assert.Equal(99, options.Headers["X-RateLimit-Remaining"]);
        Assert.True(options.Headers.ContainsKey("X-RateLimit-Reset"));
    }

    [Fact]
    public async Task PublishAsync_WhenRequestRejected_ShouldThrowRateLimitExceededException()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(5);
        var resetAt = DateTimeOffset.UtcNow.AddSeconds(10);
        _rateLimiterMock.Setup(r => r.CheckAsync(It.IsAny<string>(), default))
            .ReturnsAsync(RateLimitResult.Reject(retryAfter, resetAt));
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _rateLimiterMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RateLimitExceededException>(async () =>
            await decorator.PublishAsync(message));

        Assert.Equal(retryAfter, exception.RetryAfter);
        Assert.Equal(resetAt, exception.ResetAt);
        _innerBrokerMock.Verify(b => b.PublishAsync(It.IsAny<TestMessage>(), null, default), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WhenPerTenantLimitsEnabled_ShouldUseTenantKey()
    {
        // Arrange
        var tenantOptions = new RateLimitOptions
        {
            Enabled = true,
            EnablePerTenantLimits = true,
            RequestsPerSecond = 100
        };
        _rateLimiterMock.Setup(r => r.CheckAsync(It.IsAny<string>(), default))
            .ReturnsAsync(RateLimitResult.Allow());
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _rateLimiterMock.Object,
            Options.Create(tenantOptions),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object?> { ["TenantId"] = "tenant-123" }
        };

        // Act
        await decorator.PublishAsync(message, options);

        // Assert
        _rateLimiterMock.Verify(r => r.CheckAsync("tenant-123", default), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenPublishFails_ShouldReThrowException()
    {
        // Arrange
        _rateLimiterMock.Setup(r => r.CheckAsync(It.IsAny<string>(), default))
            .ReturnsAsync(RateLimitResult.Allow());
        _innerBrokerMock.Setup(b => b.PublishAsync(It.IsAny<TestMessage>(), null, default))
            .ThrowsAsync(new Exception("Publish failed"));
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _rateLimiterMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        var message = new TestMessage { Id = 1, Content = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await decorator.PublishAsync(message));
    }

    [Fact]
    public async Task SubscribeAsync_ShouldDelegateToInnerBroker()
    {
        // Arrange
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _rateLimiterMock.Object,
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
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _rateLimiterMock.Object,
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
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _rateLimiterMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        await decorator.StopAsync();

        // Assert
        _innerBrokerMock.Verify(b => b.StopAsync(default), Times.Once);
    }

    [Fact]
    public void GetMetrics_ShouldReturnRateLimiterMetrics()
    {
        // Arrange
        var expectedMetrics = new RateLimiterMetrics { TotalRequests = 10 };
        _rateLimiterMock.Setup(r => r.GetMetrics()).Returns(expectedMetrics);
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            _rateLimiterMock.Object,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        var metrics = decorator.GetMetrics();

        // Assert
        Assert.Equal(expectedMetrics, metrics);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeRateLimiterWhenDisposable()
    {
        // Arrange
        var disposableRateLimiter = new TestDisposableRateLimiter();
        var decorator = new RateLimitMessageBrokerDecorator(
            _innerBrokerMock.Object,
            disposableRateLimiter,
            Options.Create(_options),
            _loggerMock.Object);

        // Act
        await decorator.DisposeAsync();

        // Assert
        Assert.True(disposableRateLimiter.IsDisposed);
    }

    private class TestDisposableRateLimiter : IRateLimiter, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask<RateLimitResult> CheckAsync(string key, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(RateLimitResult.Allow());

        public RateLimiterMetrics GetMetrics() => new RateLimiterMetrics();

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}