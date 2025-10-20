using System.Reflection;
using System.Net.Sockets;
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

    private static bool IsKafkaAvailable()
    {
        try
        {
            using var tcpClient = new TcpClient();
            var result = tcpClient.BeginConnect("localhost", 9092, null, null);
            var success = result.AsyncWaitHandle.WaitOne(1000); // 1 second timeout
            tcpClient.EndConnect(result);
            return success;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task StartAsync_MultipleTimes_ShouldNotThrow()
    {
        if (!IsKafkaAvailable())
        {
            return; // Skip test if Kafka is not available
        }

        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        await broker.StartAsync();
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync()); // Start twice
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_ShouldNotThrow()
    {
        if (!IsKafkaAvailable())
        {
            return; // Skip test if Kafka is not available
        }

        // Arrange
        var options = Options.Create(_options);
        var broker = new KafkaMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        Assert.Null(exception);
    }



    [Fact]
    public async Task StopAsync_AfterStart_ShouldNotThrow()
    {
        if (!IsKafkaAvailable())
        {
            return; // Skip test if Kafka is not available
        }

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
        if (!IsKafkaAvailable())
        {
            return; // Skip test if Kafka is not available
        }

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
        if (!IsKafkaAvailable())
        {
            return; // Skip test if Kafka is not available
        }

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
        if (!IsKafkaAvailable())
        {
            return; // Skip test if Kafka is not available
        }

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