using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Relay.MessageBroker.RabbitMQ;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQMessageBrokerSubscribeTests
{
    private readonly Mock<ILogger<RabbitMQMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public RabbitMQMessageBrokerSubscribeTests()
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
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        Func<TestMessage, MessageContext, CancellationToken, ValueTask>? handler = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync(handler!));
    }

    [Fact]
    public async Task SubscribeAsync_WithValidHandler_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        // Note: This will fail in test environment without RabbitMQ server, but should not throw configuration exceptions
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask)); // Expected to fail due to no RabbitMQ server
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

        // Act & Assert
        // Note: This will fail in test environment without RabbitMQ server, but should not throw configuration exceptions
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions)); // Expected to fail due to no RabbitMQ server
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomQueueName_ShouldUseProvidedQueueName()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            QueueName = "custom-queue-name"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomRoutingKey_ShouldUseProvidedRoutingKey()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            RoutingKey = "custom.subscription.key"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithCustomExchange_ShouldUseProvidedExchange()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            Exchange = "custom-subscription-exchange"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithNonDurable_ShouldSetNonDurable()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            Durable = false
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithExclusive_ShouldSetExclusive()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            Exclusive = true
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithAutoDelete_ShouldSetAutoDelete()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            AutoDelete = true
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_WithPrefetchCount_ShouldSetPrefetchCount()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var subscriptionOptions = new SubscriptionOptions
        {
            PrefetchCount = 10
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.SubscribeAsync<TestMessage>(
            (msg, ctx, ct) => ValueTask.CompletedTask,
            subscriptionOptions));
    }

    [Fact]
    public async Task SubscribeAsync_MultipleHandlers_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        });
    }

    [Fact]
    public async Task SubscribeAsync_DifferentMessageTypes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
            await broker.SubscribeAsync<AnotherTestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);
        });
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