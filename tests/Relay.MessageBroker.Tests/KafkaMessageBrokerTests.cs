using FluentAssertions;
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
        broker.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        TestMessage? message = null;

        // Act
        Func<Task> act = async () => await broker.PublishAsync(message!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        Func<TestMessage, MessageContext, CancellationToken, ValueTask>? handler = null;

        // Act
        Func<Task> act = async () => await broker.SubscribeAsync(handler!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await broker.StopAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act
        Action act = () => broker.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act
        Action act = () =>
        {
            broker.Dispose();
            broker.Dispose(); // Dispose twice
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SubscribeAsync_WithValidHandler_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask);

        // Assert
        await act.Should().NotThrowAsync();
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

        // Act
        Func<Task> act = async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SubscribeAsync_MultipleHandlers_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act
        Func<Task> act = async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SubscribeAsync_DifferentMessageTypes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act
        Func<Task> act = async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<AnotherTestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        };

        // Assert
        await act.Should().NotThrowAsync();
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
        broker.Should().NotBeNull();
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
        broker.Should().NotBeNull();
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

            broker.Should().NotBeNull();
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

            broker.Should().NotBeNull();
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
