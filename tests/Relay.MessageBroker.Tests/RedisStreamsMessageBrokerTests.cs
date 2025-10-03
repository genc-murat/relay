using FluentAssertions;
using Relay.MessageBroker.RedisStreams;
using Moq;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RedisStreamsMessageBrokerTests
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new RedisStreamsMessageBroker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithoutRedisOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act
        Action act = () => new RedisStreamsMessageBroker(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Redis Streams options are required.");
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions { ConnectionString = "" }
        };

        // Act
        Action act = () => new RedisStreamsMessageBroker(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Redis connection string is required.");
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379",
                DefaultStreamName = "test"
            }
        };

        // Act
        var broker = new RedisStreamsMessageBroker(options);

        // Assert
        broker.Should().NotBeNull();
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            RedisStreams = new RedisStreamsOptions { ConnectionString = "localhost:6379" }
        };
        var broker = new RedisStreamsMessageBroker(options);

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
