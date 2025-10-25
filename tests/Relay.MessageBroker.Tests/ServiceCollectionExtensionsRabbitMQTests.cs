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

    [Fact]
    public void AddRabbitMQ_WithNullHostName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = null!;
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithEmptyHostName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "";
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithInvalidPort_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 0;
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithPortTooHigh_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 70000;
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithNullUserName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 5672;
                options.UserName = null!;
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithEmptyUserName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 5672;
                options.UserName = "";
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithNullPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 5672;
                options.UserName = "guest";
                options.Password = null!;
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithEmptyPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 5672;
                options.UserName = "guest";
                options.Password = "";
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithNullVirtualHost_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 5672;
                options.UserName = "guest";
                options.Password = "guest";
                options.VirtualHost = null!;
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithEmptyVirtualHost_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 5672;
                options.UserName = "guest";
                options.Password = "guest";
                options.VirtualHost = "";
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithZeroPrefetchCount_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 5672;
                options.UserName = "guest";
                options.Password = "guest";
                options.VirtualHost = "/";
                options.PrefetchCount = 0;
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithNullExchangeType_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 5672;
                options.UserName = "guest";
                options.Password = "guest";
                options.VirtualHost = "/";
                options.ExchangeType = null!;
            }));
    }

    [Fact]
    public void AddRabbitMQ_WithEmptyExchangeType_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = 5672;
                options.UserName = "guest";
                options.Password = "guest";
                options.VirtualHost = "/";
                options.ExchangeType = "";
            }));
    }
}