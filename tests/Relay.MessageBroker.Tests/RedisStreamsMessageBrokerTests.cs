using Relay.MessageBroker.RedisStreams;
using Moq;
using Xunit;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.MessageBroker;

namespace Relay.MessageBroker.Tests;

public class RedisStreamsMessageBrokerTests : IDisposable
{
    private readonly Mock<ILogger<RedisStreamsMessageBroker>> _mockLogger;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly MessageBrokerOptions _defaultOptions;

    public RedisStreamsMessageBrokerTests()
    {
        _mockLogger = new Mock<ILogger<RedisStreamsMessageBroker>>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        
        _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>())).Returns(_mockDatabase.Object);
        _mockRedis.Setup(x => x.IsConnected).Returns(true);

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
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RedisStreamsMessageBroker(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithoutRedisOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new RedisStreamsMessageBroker(Options.Create(options), _mockLogger.Object));
        Assert.Equal("Redis Streams options are required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions { ConnectionString = "" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new RedisStreamsMessageBroker(Options.Create(options), _mockLogger.Object));
        Assert.Equal("Redis connection string is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange & Act
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object);

        // Assert
        Assert.NotNull(broker);
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
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task StartAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        Assert.Null(exception); // StartAsync uses lazy connection
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_AfterStart_ShouldNotThrow()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object);
        
        // StartAsync completes without Redis connection (lazy connection)
        await broker.StartAsync();

        // Act & Assert
        // StopAsync should not throw
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void Constructor_WithDifferentDatabaseNumbers_ShouldSucceed(int database)
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379",
                Database = database,
                CreateConsumerGroupIfNotExists = false
            }
        };

        // Act
        var broker = new RedisStreamsMessageBroker(Options.Create(options), _mockLogger.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Theory]
    [InlineData("")]
    [InlineData("custom-stream")]
    [InlineData("stream:with:colons")]
    [InlineData("stream_with_underscores")]
    public void Constructor_WithDifferentStreamNames_ShouldSucceed(string streamName)
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379",
                DefaultStreamName = streamName,
                CreateConsumerGroupIfNotExists = false
            }
        };

        // Act
        var broker = new RedisStreamsMessageBroker(Options.Create(options), _mockLogger.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithMaxStreamLength_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379",
                MaxStreamLength = 100,
                CreateConsumerGroupIfNotExists = false
            }
        };

        // Act
        var broker = new RedisStreamsMessageBroker(Options.Create(options), _mockLogger.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void RedisStreamsOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new RedisStreamsOptions();

        // Assert
        Assert.Equal("relay:stream", options.DefaultStreamName);
        Assert.Equal("relay-consumer-group", options.ConsumerGroupName);
        Assert.Equal("relay-consumer", options.ConsumerName);
        Assert.Equal(0, options.Database);
        Assert.True(options.CreateConsumerGroupIfNotExists);
        Assert.True(options.AutoAcknowledge);
        Assert.Null(options.MaxStreamLength);
        Assert.Null(options.ConnectTimeout);
        Assert.Null(options.SyncTimeout);
    }

    [Fact]
    public void MessageBrokerOptions_ShouldIncludeRedisStreams()
    {
        // Arrange & Act
        var options = new MessageBrokerOptions();

        // Assert
        Assert.Null(options.RedisStreams);
    }

    [Fact]
    public void Constructor_WithCreateConsumerGroupEnabled_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379",
                CreateConsumerGroupIfNotExists = true
            }
        };

        // Act
        var broker = new RedisStreamsMessageBroker(Options.Create(options), _mockLogger.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithAutoAcknowledgeDisabled_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379",
                AutoAcknowledge = false,
                CreateConsumerGroupIfNotExists = false
            }
        };

        // Act
        var broker = new RedisStreamsMessageBroker(Options.Create(options), _mockLogger.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithCustomTimeouts_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379",
                ConnectTimeout = TimeSpan.FromSeconds(10),
                SyncTimeout = TimeSpan.FromSeconds(10),
                CreateConsumerGroupIfNotExists = false
            }
        };

        // Act
        var broker = new RedisStreamsMessageBroker(Options.Create(options), _mockLogger.Object);

        // Assert
        Assert.NotNull(broker);
    }

    public void Dispose()
    {
        // Cleanup if needed
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