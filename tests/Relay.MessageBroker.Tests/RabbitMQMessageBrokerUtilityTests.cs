using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Relay.MessageBroker.RabbitMQ;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQMessageBrokerUtilityTests
{
    private readonly Mock<ILogger<RabbitMQMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public RabbitMQMessageBrokerUtilityTests()
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
    public void GetRoutingKey_ShouldReplaceMessageTypePlaceholder()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Use reflection to access the private GetRoutingKey method
        var method = typeof(RabbitMQMessageBroker).GetMethod("GetRoutingKey", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var routingKey = (string)method.Invoke(broker, new object[] { typeof(TestMessage) })!;

        // Assert
        Assert.Equal("relay.TestMessage", routingKey);
    }

    [Fact]
    public void GetRoutingKey_WithCustomPattern_ShouldReplacePlaceholders()
    {
        // Arrange
        var customOptions = new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            },
            DefaultExchange = "relay-test",
            DefaultRoutingKeyPattern = "custom.{MessageType}.{MessageFullName}"
        };
        var options = Options.Create(customOptions);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Use reflection to access the private GetRoutingKey method
        var method = typeof(RabbitMQMessageBroker).GetMethod("GetRoutingKey", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var routingKey = (string)method.Invoke(broker, new object[] { typeof(TestMessage) })!;

        // Assert
        Assert.Equal($"custom.TestMessage.{typeof(TestMessage).FullName}", routingKey);
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