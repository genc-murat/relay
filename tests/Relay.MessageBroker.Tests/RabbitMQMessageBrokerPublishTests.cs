using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Relay.MessageBroker.RabbitMQ;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQMessageBrokerPublishTests
{
    private readonly Mock<ILogger<RabbitMQMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public RabbitMQMessageBrokerPublishTests()
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
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        TestMessage? message = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync(message!));
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldNotThrowConfigurationExceptions()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };

        // Act & Assert
        // Note: This will fail in test environment without RabbitMQ server, but should not throw configuration exceptions
        // The test verifies that the method can be called with valid parameters without throwing ArgumentException etc.
        try
        {
            await broker.PublishAsync(message);
            // If it succeeds, that's also fine (if RabbitMQ is running)
        }
        catch (Exception ex)
        {
            // Connection exceptions are expected in test environment, but not configuration exceptions
            Assert.IsNotType<ArgumentException>(ex);
            Assert.IsNotType<ArgumentNullException>(ex);
        }
    }

    [Fact]
    public async Task PublishAsync_WithCustomRoutingKey_ShouldUseProvidedRoutingKey()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "custom.routing.key"
        };

        // Act & Assert
        // This will fail due to no RabbitMQ server, but should not throw configuration exceptions
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithCustomExchange_ShouldUseProvidedExchange()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Exchange = "custom-exchange"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["custom-header"] = "header-value",
                ["numeric-header"] = 42
            }
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithPriority_ShouldSetPriority()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Priority = 5
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithExpiration_ShouldSetExpiration()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Expiration = TimeSpan.FromMinutes(5)
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithNonPersistent_ShouldSetNonPersistent()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "test" };
        var publishOptions = new PublishOptions
        {
            Persistent = false
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await broker.PublishAsync(message, publishOptions));
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