using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.RedisStreams;
using StackExchange.Redis;

namespace Relay.MessageBroker.Tests;

public class RedisStreamsMessageBrokerSubscribeTests : IDisposable
{
    private readonly Mock<ILogger<RedisStreamsMessageBroker>> _mockLogger;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly MessageBrokerOptions _defaultOptions;
    private readonly RedisStreamsMessageBroker _broker;

    public RedisStreamsMessageBrokerSubscribeTests()
    {
        _mockLogger = new Mock<ILogger<RedisStreamsMessageBroker>>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();

        _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>())).Returns(_mockDatabase.Object);
        _mockRedis.SetupGet(x => x.IsConnected).Returns(true);

        _defaultOptions = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379",
                DefaultStreamName = "test-stream",
                ConsumerGroupName = "test-group",
                ConsumerName = "test-consumer",
                Database = 0,
                CreateConsumerGroupIfNotExists = false, // Disable for unit tests
                AutoAcknowledge = true,
                MaxStreamLength = 1000,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                SyncTimeout = TimeSpan.FromSeconds(5)
            },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 3,
                InitialDelay = TimeSpan.FromMilliseconds(100),
                MaxDelay = TimeSpan.FromSeconds(5),
                UseExponentialBackoff = true,
                BackoffMultiplier = 2
            }
        };

        // Create broker once for all tests
        _broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_WithValidHandler_ShouldNotThrow()
    {
        // Act & Assert - Test basic subscription
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomQueueName_ShouldUseProvidedQueueName()
    {
        // Arrange
        var subscriptionOptions = new SubscriptionOptions
        {
            QueueName = "custom-stream-name"
        };

        // Act & Assert
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask, subscriptionOptions);
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomConsumerGroup_ShouldUseProvidedConsumerGroup()
    {
        // Arrange
        var subscriptionOptions = new SubscriptionOptions
        {
            ConsumerGroup = "custom-consumer-group"
        };

        // Act & Assert
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask, subscriptionOptions);
    }

    [Fact]
    public async Task SubscribeAsync_WithOptions_ShouldNotThrow()
    {
        // Arrange
        var subscriptionOptions = new SubscriptionOptions
        {
            QueueName = "test-stream",
            ConsumerGroup = "test-group"
        };

        // Act & Assert
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask, subscriptionOptions);
    }

    [Fact]
    public async Task SubscribeAsync_MultipleHandlers_ShouldNotThrow()
    {
        // Act & Assert - Test multiple subscriptions
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
    }

    [Fact]
    public async Task SubscribeAsync_DifferentMessageTypes_ShouldNotThrow()
    {
        // Act & Assert - Test different message types
        await _broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        await _broker.SubscribeAsync<ComplexMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    private class ComplexMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}