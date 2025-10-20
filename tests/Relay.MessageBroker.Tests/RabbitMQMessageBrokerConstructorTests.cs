using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Relay.MessageBroker.RabbitMQ;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQMessageBrokerConstructorTests
{
    private readonly Mock<ILogger<RabbitMQMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public RabbitMQMessageBrokerConstructorTests()
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
        Assert.NotNull(broker);
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