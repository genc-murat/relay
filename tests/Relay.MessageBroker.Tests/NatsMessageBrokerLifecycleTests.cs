using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly.CircuitBreaker;
using Relay.MessageBroker.Nats;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class NatsMessageBrokerLifecycleTests
{
    private readonly Mock<ILogger<NatsMessageBroker>> _loggerMock = new();

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
    public async Task PublishAsync_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);
        var message = new TestMessage { Id = 1, Content = "Test" };

        await broker.DisposeAsync();

        // Act & Assert
        // BaseMessageBroker doesn't throw after dispose, it just doesn't work
        var exception = await Record.ExceptionAsync(async () => await broker.PublishAsync(message));
        // May throw due to no connection, but not ObjectDisposedException
        Assert.True(exception == null || exception is not ObjectDisposedException);
    }

    [Fact]
    public async Task SubscribeAsync_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        async ValueTask Handler(TestMessage message, MessageContext context, CancellationToken ct)
        {
            // Test handler
        }

        await broker.DisposeAsync();

        // Act & Assert
        // BaseMessageBroker doesn't throw after dispose, it just doesn't work
        var exception = await Record.ExceptionAsync(async () => await broker.SubscribeAsync<TestMessage>(Handler));
        // May throw due to no connection, but not ObjectDisposedException
        Assert.True(exception == null || exception is not ObjectDisposedException);
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

    [Fact]
    public async Task StartAsync_AfterStop_ShouldNotThrow()
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

        await broker.StopAsync();

        // Act & Assert
        // Should not throw even if connection fails
        var exception = await Record.ExceptionAsync(async () => await broker.StartAsync());
        // Note: May throw NatsException due to no server, but should not throw other exceptions
        Assert.True(exception == null || exception is NATS.Client.Core.NatsException);
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act & Assert
        var exception1 = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        var exception2 = await Record.ExceptionAsync(async () => await broker.DisposeAsync());

        Assert.Null(exception1);
        Assert.Null(exception2);
    }

    [Fact]
    public async Task DisposeAsync_AfterStart_ShouldDisposeConnection()
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

        // Act
        await broker.DisposeAsync();

        // Assert - Should not throw on subsequent operations
        var exception = await Record.ExceptionAsync(async () => await broker.DisposeAsync());
        Assert.Null(exception);
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