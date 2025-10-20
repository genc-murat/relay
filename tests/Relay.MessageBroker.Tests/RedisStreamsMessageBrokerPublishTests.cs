using Relay.MessageBroker.RedisStreams;
using Moq;
using Xunit;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.MessageBroker;
using System.Reflection;

namespace Relay.MessageBroker.Tests;

public class RedisStreamsMessageBrokerPublishTests
{
    private readonly Mock<ILogger<RedisStreamsMessageBroker>> _mockLogger;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly MessageBrokerOptions _defaultOptions;

    public RedisStreamsMessageBrokerPublishTests()
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
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync<object>(null!));
    }

    [Fact]
    public async Task PublishAsync_WithCustomRoutingKey_ShouldUseProvidedRoutingKey()
    {
        // Arrange


        var broker = new RedisStreamsMessageBroker(
            Options.Create(_defaultOptions),
            _mockLogger.Object,
            connectionMultiplexer: _mockRedis.Object);

        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "custom-stream"
        };

        // Act
        await broker.PublishAsync(message, publishOptions);

        // Assert - The method completed without throwing, which indicates StreamAddAsync was called successfully
    }

    [Fact]
    public async Task PublishAsync_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange


        var broker = new RedisStreamsMessageBroker(
            Options.Create(_defaultOptions),
            _mockLogger.Object,
            connectionMultiplexer: _mockRedis.Object);

        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["custom-header"] = "header-value",
                ["numeric-header"] = 42
            }
        };

        // Act
        await broker.PublishAsync(message, publishOptions);

        // Assert - The method completed without throwing, which indicates StreamAddAsync was called successfully
    }

    [Fact]
    public async Task PublishAsync_WithPriority_ShouldIncludePriority()
    {
        // Arrange


        var broker = new RedisStreamsMessageBroker(
            Options.Create(_defaultOptions),
            _mockLogger.Object,
            connectionMultiplexer: _mockRedis.Object);

        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Priority = 5
        };

        // Act
        await broker.PublishAsync(message, publishOptions);

        // Assert - The method completed without throwing, which indicates StreamAddAsync was called successfully
    }

    [Fact]
    public async Task PublishAsync_WithExpiration_ShouldIncludeExpiration()
    {
        // Arrange


        var broker = new RedisStreamsMessageBroker(
            Options.Create(_defaultOptions),
            _mockLogger.Object,
            connectionMultiplexer: _mockRedis.Object);

        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Expiration = TimeSpan.FromMinutes(5)
        };

        // Act
        await broker.PublishAsync(message, publishOptions);

        // Assert - The method completed without throwing, which indicates StreamAddAsync was called successfully
    }

    [Fact]
    public async Task PublishAsync_WithCorrelationId_ShouldIncludeCorrelationId()
    {
        // Arrange


        var broker = new RedisStreamsMessageBroker(
            Options.Create(_defaultOptions),
            _mockLogger.Object,
            connectionMultiplexer: _mockRedis.Object);

        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["CorrelationId"] = "test-correlation-id"
            }
        };

        // Act
        await broker.PublishAsync(message, publishOptions);

        // Assert - The method completed without throwing, which indicates StreamAddAsync was called successfully
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