using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Relay.MessageBroker.RabbitMQ;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQMessageBrokerMessageProcessingTests
{
    private readonly Mock<ILogger<RabbitMQMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public RabbitMQMessageBrokerMessageProcessingTests()
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
    public async Task CreateMessageContext_Acknowledge_ShouldCallBasicAckAsync()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        var channelMock = new Mock<IChannel>();
        var basicProperties = new BasicProperties
        {
            MessageId = "test-message-id",
            CorrelationId = "test-correlation-id",
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                ["header1"] = "value1",
                ["header2"] = 42
            }
        };

        var body = ReadOnlyMemory<byte>.Empty;
        var ea = new BasicDeliverEventArgs("consumer-tag", 123, false, "test-exchange", "test.routing.key", basicProperties, body, CancellationToken.None);

        // Use reflection to access the private CreateMessageContext method
        var method = typeof(RabbitMQMessageBroker).GetMethod("CreateMessageContext", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var context = (MessageContext)method.Invoke(broker, new object[] { ea, channelMock.Object })!;

        // Act
        await context.Acknowledge();

        // Assert
        channelMock.Verify(c => c.BasicAckAsync(123, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMessageContext_Reject_ShouldCallBasicNackAsync()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        var channelMock = new Mock<IChannel>();
        var basicProperties = new BasicProperties
        {
            MessageId = "test-message-id"
        };

        var body = ReadOnlyMemory<byte>.Empty;
        var ea = new BasicDeliverEventArgs("consumer-tag", 123, false, "test-exchange", "test.routing.key", basicProperties, body, CancellationToken.None);

        // Use reflection to access the private CreateMessageContext method
        var method = typeof(RabbitMQMessageBroker).GetMethod("CreateMessageContext", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var context = (MessageContext)method.Invoke(broker, new object[] { ea, channelMock.Object })!;

        // Act
        await context.Reject(true);

        // Assert
        channelMock.Verify(c => c.BasicNackAsync(123, false, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateMessageContext_WithNullHeaders_ShouldHandleNullHeaders()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        var channelMock = new Mock<IChannel>();
        var basicProperties = new BasicProperties
        {
            MessageId = "test-message-id",
            Headers = null
        };

        var body = ReadOnlyMemory<byte>.Empty;
        var ea = new BasicDeliverEventArgs("consumer-tag", 123, false, "test-exchange", "test.routing.key", basicProperties, body, CancellationToken.None);

        // Use reflection to access the private CreateMessageContext method
        var method = typeof(RabbitMQMessageBroker).GetMethod("CreateMessageContext", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var context = (MessageContext)method.Invoke(broker, new object[] { ea, channelMock.Object })!;

        // Assert
        Assert.Null(context.Headers);
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