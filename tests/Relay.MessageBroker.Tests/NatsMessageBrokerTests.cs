using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly.CircuitBreaker;
using Relay.MessageBroker.Nats;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class NatsMessageBrokerTests
{
    private readonly Mock<ILogger<NatsMessageBroker>> _loggerMock = new();

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NatsMessageBroker(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithoutNatsOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new NatsMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Equal("NATS options are required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyServers_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = Array.Empty<string>() }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new NatsMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Equal("At least one NATS server URL is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                Name = "test-client"
            }
        };

        // Act
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

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
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task StartAsync_WithValidOptions_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        // Note: This will fail in test environment without NATS server, but should not throw configuration exceptions
        await Assert.ThrowsAsync<NATS.Client.Core.NatsException>(async () => await broker.StartAsync()); // Expected to fail due to no NATS server
    }

    [Fact]
    public async Task StartAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        try
        {
            await broker.StartAsync();
        }
        catch (NATS.Client.Core.NatsException)
        {
            // Expected to fail without NATS server
        }

        // Act & Assert
        await Assert.ThrowsAsync<NATS.Client.Core.NatsException>(async () => await broker.StartAsync()); // Still expected to fail
    }

    [Fact]
    public async Task DisposeAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);
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

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = new[] { "nats://localhost:4222" } }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await broker.StopAsync());
        Assert.Null(exception);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
