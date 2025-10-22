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

public class RedisStreamsMessageBrokerLifecycleTests
{
    private readonly Mock<ILogger<RedisStreamsMessageBroker>> _mockLogger;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly MessageBrokerOptions _defaultOptions;

    public RedisStreamsMessageBrokerLifecycleTests()
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
    public async Task StartAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        Assert.Null(exception); // StartAsync uses lazy connection
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_AfterStart_ShouldNotThrow()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

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
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);

    }

    [Fact]
    public async Task StartAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.StartAsync();
            await broker.StartAsync(); // Call twice
        });
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.DisposeAsync();
            await broker.DisposeAsync(); // Dispose twice
        });
        Assert.Null(exception);
    }

    #region Internal Method Tests

    [Fact]
    public async Task StartInternalAsync_ShouldEnsureConnectionAndLog()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        // Use reflection to access the protected method
        var startInternalMethod = typeof(RedisStreamsMessageBroker).GetMethod("StartInternalAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Act
        await (ValueTask)startInternalMethod.Invoke(broker, new object[] { CancellationToken.None })!;

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Redis Streams message broker started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopInternalAsync_ShouldCancelConsumerTasksAndDisposeResourcesAndLog()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        // Add a mock consumer task to the dictionary
        var consumerTasksField = typeof(RedisStreamsMessageBroker).GetField("_consumerTasks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var consumerTasks = consumerTasksField!.GetValue(broker) as Dictionary<string, CancellationTokenSource>;
        var cts = new CancellationTokenSource();
        consumerTasks!["test-stream"] = cts;

        // Use reflection to access the protected method
        var stopInternalMethod = typeof(RedisStreamsMessageBroker).GetMethod("StopInternalAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Act
        await (ValueTask)stopInternalMethod.Invoke(broker, new object[] { CancellationToken.None })!;

        // Assert
        Assert.True(cts.IsCancellationRequested);
        Assert.Empty(consumerTasks);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Redis Streams message broker stopped")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DisposeInternalAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        // Note: ConnectionMultiplexer is sealed and cannot be mocked, so we use the mock IConnectionMultiplexer
        // The type check in DisposeInternalAsync (if (_redis is ConnectionMultiplexer)) will fail,
        // but the method should still complete without throwing an error
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        // Use reflection to access the protected method
        var disposeInternalMethod = typeof(RedisStreamsMessageBroker).GetMethod("DisposeInternalAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Act & Assert - Should complete without throwing
        var exception = await Record.ExceptionAsync(async () =>
            await (ValueTask)disposeInternalMethod.Invoke(broker, Array.Empty<object>())!);

        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishInternalAsync_ShouldAddMessageToStreamWithHeaders()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        var message = new TestMessage { Id = 1, Content = "Test", Timestamp = DateTime.UtcNow };
        var serializedMessage = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);
        var publishOptions = new PublishOptions
        {
            RoutingKey = "custom:stream",
            Headers = new Dictionary<string, object?> { { "CorrelationId", "test-correlation" }, { "CustomHeader", "CustomValue" } },
            Priority = 5,
            Expiration = TimeSpan.FromMinutes(10)
        };

        // Mock StreamAddAsync to return a message ID
        // Signature: StreamAddAsync(RedisKey key, NameValueEntry[] streamPairs, RedisValue? messageId, long? maxLength, bool useApproximateMaxLength, long? limit, StreamTrimMode trimMode, CommandFlags flags)
        _mockDatabase
            .Setup(d => d.StreamAddAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<NameValueEntry[]>(),
                It.IsAny<RedisValue?>(),
                It.IsAny<long?>(),
                It.IsAny<bool>(),
                It.IsAny<long?>(),
                It.IsAny<StreamTrimMode>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)"123456789-0");

        // Mock StreamTrimAsync (called after publish to trim the stream)
        _mockDatabase
            .Setup(d => d.StreamTrimAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<bool>(),
                It.IsAny<long?>(),
                It.IsAny<StreamTrimMode>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1L);

        // Use reflection to access the protected generic method
        var publishInternalMethod = typeof(RedisStreamsMessageBroker)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .First(m => m.Name == "PublishInternalAsync" && m.IsGenericMethod);

        var genericMethod = publishInternalMethod.MakeGenericMethod(typeof(TestMessage));

        // Act
        await (ValueTask)genericMethod.Invoke(broker, new object[] { message, serializedMessage, publishOptions, CancellationToken.None })!;

        // Assert - Verify StreamAddAsync was called with expected stream name and entries
        _mockDatabase.Verify(d => d.StreamAddAsync(
            "custom:stream",
            It.Is<NameValueEntry[]>(entries =>
                entries.Any(e => e.Name == "type" && e.Value == "Relay.MessageBroker.Tests.RedisStreamsMessageBrokerLifecycleTests+TestMessage") &&
                entries.Any(e => e.Name == "correlationId" && e.Value == "test-correlation") &&
                entries.Any(e => e.Name == "header:CustomHeader" && e.Value == "CustomValue") &&
                entries.Any(e => e.Name == "priority" && e.Value == "5") &&
                entries.Any(e => e.Name == "expiration" && e.Value == "600000")),
            It.IsAny<RedisValue?>(),
            It.IsAny<long?>(),
            It.IsAny<bool>(),
            It.IsAny<long?>(),
            It.IsAny<StreamTrimMode>(),
            It.IsAny<CommandFlags>()), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Published message TestMessage with ID 123456789-0 to Redis stream custom:stream")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SubscribeInternalAsync_ShouldCreateConsumerTask()
    {
        // Arrange
        var broker = new RedisStreamsMessageBroker(Options.Create(_defaultOptions), _mockLogger.Object, connectionMultiplexer: _mockRedis.Object);

        // Use reflection to access the protected method
        var subscribeInternalMethod = typeof(RedisStreamsMessageBroker).GetMethod("SubscribeInternalAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var subscriptionInfo = new SubscriptionInfo
        {
            MessageType = typeof(TestMessage),
            Handler = (msg, ctx, ct) => ValueTask.CompletedTask,
            Options = new SubscriptionOptions()
        };

        // Act
        await (ValueTask)subscribeInternalMethod.Invoke(broker, new object[] { typeof(TestMessage), subscriptionInfo, CancellationToken.None })!;

        // Assert - Consumer task should be added to the dictionary
        var consumerTasksField = typeof(RedisStreamsMessageBroker).GetField("_consumerTasks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var consumerTasks = consumerTasksField!.GetValue(broker) as Dictionary<string, CancellationTokenSource>;
        Assert.Single(consumerTasks!);
        Assert.Contains("test-stream", consumerTasks.Keys);
    }

    #endregion

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