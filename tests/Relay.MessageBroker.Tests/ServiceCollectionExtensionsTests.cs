using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageBroker_ShouldRegisterIMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672
            };
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
    }

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
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<RabbitMQ.RabbitMQMessageBroker>();
    }

    [Fact]
    public void AddKafka_ShouldRegisterKafkaMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafka(options =>
        {
            options.BootstrapServers = "localhost:9092";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<Kafka.KafkaMessageBroker>();
    }

    [Fact]
    public void AddMessageBrokerHostedService_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMQ();

        // Act
        services.AddMessageBrokerHostedService();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        hostedServices.Should().Contain(s => s is MessageBrokerHostedService);
    }

    [Fact]
    public void AddMessageBroker_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddMessageBroker(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMessageBroker_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddMessageBroker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddRabbitMQ_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddRabbitMQ();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddKafka_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddKafka();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMessageBrokerHostedService_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddMessageBrokerHostedService();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMessageBroker_WithUnsupportedBrokerType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMessageBroker(options =>
        {
            options.BrokerType = (MessageBrokerType)999; // Invalid broker type
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        Action act = () => serviceProvider.GetRequiredService<IMessageBroker>();

        // Assert
        act.Should().Throw<NotSupportedException>();
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
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<RabbitMQ.RabbitMQMessageBroker>();
    }

    [Fact]
    public void AddKafka_WithoutConfiguration_ShouldUseDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafka();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<Kafka.KafkaMessageBroker>();
    }
}
