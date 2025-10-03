using FluentAssertions;
using Relay.MessageBroker.Nats;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class NatsMessageBrokerTests
{
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
