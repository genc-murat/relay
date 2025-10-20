using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Kafka;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class KafkaMessageBrokerLifecycleTests
{
    private readonly Mock<ILogger<KafkaMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public KafkaMessageBrokerLifecycleTests()
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
    public async Task StartAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.StartAsync();
            await broker.StartAsync(); // Start twice
        });
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_AfterStart_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        await broker.StartAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);
        await broker.StartAsync();
        await broker.StopAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_WithSubscriptions_ShouldSetupConsumers()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        await broker.SubscribeAsync<AnotherTestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_WithActiveConsumers_ShouldCloseConsumers()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        await broker.StartAsync();

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