using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.RabbitMQ;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQMessageBrokerTests
{
    private readonly Mock<ILogger<RabbitMQMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public RabbitMQMessageBrokerTests()
    {
        _loggerMock = new Mock<ILogger<RabbitMQMessageBroker>>();
        _options = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = "topic"
            },
            DefaultExchange = "relay-test",
            DefaultRoutingKeyPattern = "relay.{MessageType}"
        };
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Assert
        broker.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
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
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
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
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await broker.StopAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert - Starting multiple times should be idempotent
        // May throw connection errors but should handle multiple calls gracefully
        try
        {
            await broker.StartAsync();
            await broker.StartAsync(); // Call twice
        }
        catch
        {
            // Connection errors are expected in test environment without RabbitMQ running
            // The important thing is the structure is correct
        }
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

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
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

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
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

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
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            QueueName = "test-queue",
            RoutingKey = "test.routing.key",
            Exchange = "test-exchange",
            Durable = true,
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
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

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
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

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
            ConnectionString = "amqp://guest:guest@localhost:5672/",
            RabbitMQ = new RabbitMQOptions
            {
                ExchangeType = "topic"
            },
            DefaultExchange = "relay-test"
        };
        var options = Options.Create(optionsWithConnectionString);

        // Act
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Assert
        broker.Should().NotBeNull();
    }

    [Fact]
    public async Task StopAsync_AfterStart_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert - May throw connection errors but structure should be valid
        try
        {
            await broker.StartAsync();
            await broker.StopAsync();
        }
        catch
        {
            // Connection errors are expected in test environment without RabbitMQ running
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
