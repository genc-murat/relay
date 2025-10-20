using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RedisStreamsValidationTests
{
    [Fact]
    public void RedisStreamsOptions_WithInvalidConnectionString_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConnectionString = ""; // Empty connection string
            }));

        Assert.Contains("ConnectionString", exception.Message);
    }

    [Fact]
    public void RedisStreamsOptions_WithInvalidDefaultStreamName_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.DefaultStreamName = ""; // Empty default stream name
            }));

        Assert.Contains("DefaultStreamName", exception.Message);
    }

    [Fact]
    public void RedisStreamsOptions_WithInvalidConsumerGroupName_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConsumerGroupName = ""; // Empty consumer group name
            }));

        Assert.Contains("ConsumerGroupName", exception.Message);
    }

    [Fact]
    public void RedisStreamsOptions_WithInvalidConsumerName_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConsumerName = ""; // Empty consumer name
            }));

        Assert.Contains("ConsumerName", exception.Message);
    }
}