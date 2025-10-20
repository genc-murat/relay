using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsRabbitMQTests
{
    [Fact]
    public void AddRabbitMQ_ShouldRegisterRabbitMQMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRabbitMQ(options =>
        {
            options.HostName = "localhost";
            options.Port = 5672;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(messageBroker);
        Assert.IsType<RabbitMQ.RabbitMQMessageBroker>(messageBroker);
    }

    [Fact]
    public void AddRabbitMQ_WithoutConfiguration_ShouldUseDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRabbitMQ();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(messageBroker);
        Assert.IsType<RabbitMQ.RabbitMQMessageBroker>(messageBroker);
    }

    [Fact]
    public void AddRabbitMQ_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddRabbitMQ());
    }

    [Fact]
    public void AddRabbitMQ_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRabbitMQ(options =>
        {
            options.HostName = "testhost";
            options.Port = 1234;
        });

        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MessageBrokerOptions>>();

        // Assert
        Assert.Equal(MessageBrokerType.RabbitMQ, configuredOptions.Value.BrokerType);
        Assert.NotNull(configuredOptions.Value.RabbitMQ);
        Assert.Equal("testhost", configuredOptions.Value.RabbitMQ.HostName);
        Assert.Equal(1234, configuredOptions.Value.RabbitMQ.Port);
    }
}