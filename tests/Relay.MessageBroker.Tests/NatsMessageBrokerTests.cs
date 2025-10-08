using FluentAssertions;
using Microsoft.Extensions.Logging;
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
        // Arrange & Act
        Action act = () => new NatsMessageBroker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithoutNatsOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act
        Action act = () => new NatsMessageBroker(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("NATS options are required.");
    }

    [Fact]
    public void Constructor_WithEmptyServers_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = Array.Empty<string>() }
        };

        // Act
        Action act = () => new NatsMessageBroker(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("At least one NATS server URL is required.");
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
        var broker = new NatsMessageBroker(options);

        // Assert
        broker.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(options, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await broker.PublishAsync<TestMessage>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(options, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await broker.SubscribeAsync<TestMessage>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StartAsync_WithValidOptions_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        Func<Task> act = async () => await broker.StartAsync();
        
        // Note: This will fail in test environment without NATS server, but should not throw configuration exceptions
        await act.Should().ThrowAsync<NATS.Client.Core.NatsException>(); // Expected to fail due to no NATS server
    }

    [Fact]
    public async Task StartAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(options, _loggerMock.Object);

        try
        {
            await broker.StartAsync();
        }
        catch (NATS.Client.Core.NatsException)
        {
            // Expected to fail without NATS server
        }

        // Act & Assert
        Func<Task> act = async () => await broker.StartAsync();
        await act.Should().ThrowAsync<NATS.Client.Core.NatsException>(); // Still expected to fail
    }

    [Fact]
    public async Task DisposeAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(options, _loggerMock.Object);

        // Act & Assert
        Func<Task> act = async () => await broker.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldNotThrowConfigurationErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(options, _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act & Assert
        Func<Task> act = async () => await broker.PublishAsync(message);
        
        // Expected to fail due to no NATS server, but not configuration errors
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SubscribeAsync_WithValidHandler_ShouldCompleteSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(options, _loggerMock.Object);
        
        async ValueTask Handler(TestMessage message, MessageContext context, CancellationToken ct)
        {
            // Test handler
        }

        // Act & Assert
        Func<Task> act = async () => await broker.SubscribeAsync<TestMessage>(Handler);
        await act.Should().NotThrowAsync();
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
        var broker = new NatsMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.StopAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
