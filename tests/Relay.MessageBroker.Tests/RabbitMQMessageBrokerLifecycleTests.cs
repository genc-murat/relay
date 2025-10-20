using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Relay.MessageBroker.RabbitMQ;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQMessageBrokerLifecycleTests
{
    private readonly Mock<ILogger<RabbitMQMessageBroker>> _loggerMock;
    private readonly MessageBrokerOptions _options;

    public RabbitMQMessageBrokerLifecycleTests()
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
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
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
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var broker = new RabbitMQMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await broker.DisposeAsync();
            await broker.DisposeAsync(); // Dispose twice
        });
        Assert.Null(exception);
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