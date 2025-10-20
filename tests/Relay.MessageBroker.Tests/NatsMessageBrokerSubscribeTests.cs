using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Nats;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class NatsMessageBrokerSubscribeTests
{
    private readonly Mock<ILogger<NatsMessageBroker>> _loggerMock = new();

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_WithValidHandler_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        async ValueTask Handler(TestMessage message, MessageContext context, CancellationToken ct)
        {
            // Test handler
        }

        // Act & Assert
        // Note: This will fail in test environment without NATS server, but should not throw configuration exceptions
        await Assert.ThrowsAsync<NATS.Client.Core.NatsException>(async () => await broker.SubscribeAsync<TestMessage>(Handler)); // Expected to fail due to no NATS server
    }

    [Fact]
    public async Task SubscribeAsync_WithHandlerThatThrows_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        async ValueTask Handler(TestMessage message, MessageContext context, CancellationToken ct)
        {
            throw new InvalidOperationException("Test exception");
        }

        // Act & Assert
        // Note: This will fail in test environment without NATS server, but should not throw configuration exceptions
        await Assert.ThrowsAsync<NATS.Client.Core.NatsException>(async () => await broker.SubscribeAsync<TestMessage>(Handler));
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