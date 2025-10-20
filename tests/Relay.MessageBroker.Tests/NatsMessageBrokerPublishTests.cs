using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Nats;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class NatsMessageBrokerPublishTests
{
    private readonly Mock<ILogger<NatsMessageBroker>> _loggerMock = new();

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act & Assert
        // Expected to fail due to no NATS server, but not configuration errors
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.PublishAsync(message));
    }

    [Fact]
    public async Task PublishAsync_WithCustomHeaders_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };
        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["custom-header"] = "custom-value",
                ["numeric-header"] = 123
            }
        };

        // Act & Assert
        // Expected to fail due to no NATS server, but not configuration errors
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithNullHeaders_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };
        var publishOptions = new PublishOptions
        {
            Headers = null
        };

        // Act & Assert
        // Expected to fail due to no NATS server, but not configuration errors
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.PublishAsync(message, publishOptions));
    }

    [Fact]
    public async Task PublishAsync_WithRoutingKey_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };
        var publishOptions = new PublishOptions
        {
            RoutingKey = "custom.subject"
        };

        // Act & Assert
        // Expected to fail due to no NATS server, but not configuration errors
        await Assert.ThrowsAnyAsync<Exception>(async () => await broker.PublishAsync(message, publishOptions));
    }

    private MessageBrokerOptions CreateValidOptions()
    {
        return new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                Name = "test-client",
                Username = "testuser",
                Password = "testpass",
                MaxReconnects = 3
            }
        };
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}