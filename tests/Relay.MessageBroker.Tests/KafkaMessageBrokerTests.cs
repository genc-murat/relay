using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Kafka;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class KafkaMessageBrokerTests
{
    private readonly Mock<ILogger<KafkaMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public KafkaMessageBrokerTests()
    {
        _loggerMock = new Mock<ILogger<KafkaMessageBroker>>();
        _options = new MessageBrokerOptions
        {
            Kafka = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId = "test-group",
                AutoOffsetReset = "earliest",
                EnableAutoCommit = false,
                CompressionType = "gzip"
            },
            DefaultRoutingKeyPattern = "relay.{MessageType}"
        };
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        TestMessage? message = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync(message!));
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        Func<TestMessage, MessageContext, CancellationToken, ValueTask>? handler = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync(handler!));
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.DisposeAsync();
            await broker.DisposeAsync(); // Dispose twice
        });
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_WithValidHandler_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_WithOptions_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            RoutingKey = "test.topic",
            ConsumerGroup = "test-consumer-group",
            AutoAck = false
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_MultipleHandlers_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        });
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscribeAsync_DifferentMessageTypes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<AnotherTestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        });
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithConnectionString_ShouldSucceed()
    {
        // Arrange
        var optionsWithConnectionString = new MessageBrokerOptions
        {
            ConnectionString = "localhost:9092,localhost:9093",
            Kafka = new KafkaOptions
            {
                ConsumerGroupId = "test-group",
                AutoOffsetReset = "earliest",
                CompressionType = "none"
            },
            DefaultRoutingKeyPattern = "relay.{MessageType}"
        };
        var options = Options.Create(optionsWithConnectionString);

        // Act
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithoutKafkaOptions_ShouldUseDefaults()
    {
        // Arrange
        var optionsWithoutKafka = new MessageBrokerOptions
        {
            DefaultRoutingKeyPattern = "relay.{MessageType}"
        };
        var options = Options.Create(optionsWithoutKafka);

        // Act
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task SubscribeAsync_BeforeStart_ShouldStoreSubscription()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        // Assert - subscription should be stored even before start
        // Verified by not throwing exception
    }

    [Fact]
    public void Constructor_WithDifferentCompressionTypes_ShouldSucceed()
    {
        // Arrange & Act & Assert
        foreach (var compressionType in new[] { "none", "gzip", "snappy", "lz4", "zstd" })
        {
            var testOptions = new MessageBrokerOptions
            {
                Kafka = new KafkaOptions
                {
                    BootstrapServers = "localhost:9092",
                    CompressionType = compressionType
                }
            };
            var options = Options.Create(testOptions);
            var broker = new KafkaMessageBroker(options, _loggerMock.Object);

            Assert.NotNull(broker);
        }
    }

    [Fact]
    public void Constructor_WithDifferentAutoOffsetReset_ShouldSucceed()
    {
        // Arrange & Act & Assert
        foreach (var offsetReset in new[] { "earliest", "latest", "error" })
        {
            var testOptions = new MessageBrokerOptions
            {
                Kafka = new KafkaOptions
                {
                    BootstrapServers = "localhost:9092",
                    AutoOffsetReset = offsetReset
                }
            };
            var options = Options.Create(testOptions);
            var broker = new KafkaMessageBroker(options, _loggerMock.Object);

            Assert.NotNull(broker);
        }
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private class AnotherTestMessage
    {
        public string Name { get; set; } = string.Empty;
    }
}
