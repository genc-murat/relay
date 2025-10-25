using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsRedisStreamsTests
{
    [Fact]
    public void AddMessageBroker_WithRedisStreams_ShouldRegisterCorrectBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RedisStreams;
            options.RedisStreams = new RedisStreamsOptions
            {
                ConnectionString = "localhost:6379"
            };
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(messageBroker);
    }

    [Fact]
    public void AddRedisStreams_ShouldRegisterRedisStreamsMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRedisStreams(options =>
        {
            options.ConnectionString = "localhost:6379";
            options.DefaultStreamName = "test-stream";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(messageBroker);
        Assert.IsType<RedisStreams.RedisStreamsMessageBroker>(messageBroker);
    }

    [Fact]
    public void AddRedisStreams_WithoutConfiguration_ShouldUseDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRedisStreams();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(messageBroker);
        Assert.IsType<RedisStreams.RedisStreamsMessageBroker>(messageBroker);
    }

    [Fact]
    public void AddRedisStreams_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddRedisStreams());
    }

    [Fact]
    public void AddRedisStreams_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRedisStreams(options =>
        {
            options.ConnectionString = "testserver:6379";
            options.DefaultStreamName = "test-stream";
        });

        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MessageBrokerOptions>>();

        // Assert
        Assert.Equal(MessageBrokerType.RedisStreams, configuredOptions.Value.BrokerType);
        Assert.NotNull(configuredOptions.Value.RedisStreams);
        Assert.Equal("testserver:6379", configuredOptions.Value.RedisStreams.ConnectionString);
        Assert.Equal("test-stream", configuredOptions.Value.RedisStreams.DefaultStreamName);
    }

    [Fact]
    public void AddRedisStreams_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConnectionString = null!;
            }));
    }

    [Fact]
    public void AddRedisStreams_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConnectionString = "";
            }));
    }

    [Fact]
    public void AddRedisStreams_WithNullDefaultStreamName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConnectionString = "localhost:6379";
                options.DefaultStreamName = null!;
            }));
    }

    [Fact]
    public void AddRedisStreams_WithEmptyDefaultStreamName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConnectionString = "localhost:6379";
                options.DefaultStreamName = "";
            }));
    }

    [Fact]
    public void AddRedisStreams_WithNullConsumerGroupName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConnectionString = "localhost:6379";
                options.DefaultStreamName = "test-stream";
                options.ConsumerGroupName = null!;
            }));
    }

    [Fact]
    public void AddRedisStreams_WithEmptyConsumerGroupName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConnectionString = "localhost:6379";
                options.DefaultStreamName = "test-stream";
                options.ConsumerGroupName = "";
            }));
    }

    [Fact]
    public void AddRedisStreams_WithNullConsumerName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConnectionString = "localhost:6379";
                options.DefaultStreamName = "test-stream";
                options.ConsumerGroupName = "test-group";
                options.ConsumerName = null!;
            }));
    }

    [Fact]
    public void AddRedisStreams_WithEmptyConsumerName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRedisStreams(options =>
            {
                options.ConnectionString = "localhost:6379";
                options.DefaultStreamName = "test-stream";
                options.ConsumerGroupName = "test-group";
                options.ConsumerName = "";
            }));
    }
}