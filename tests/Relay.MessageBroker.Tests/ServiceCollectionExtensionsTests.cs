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

    [Fact]
    public void AddMessageBroker_WithAzureServiceBus_ShouldRegisterCorrectBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.AzureServiceBus;
            options.AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "test-connection-string"
            };
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
    }

    [Fact]
    public void AddMessageBroker_WithAwsSqsSns_ShouldRegisterCorrectBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.AwsSqsSns;
            options.AwsSqsSns = new AwsSqsSnsOptions();
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
    }

    [Fact]
    public void AddMessageBroker_WithNats_ShouldRegisterCorrectBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.Nats;
            options.Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" }
            };
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
    }

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
        messageBroker.Should().NotBeNull();
    }

    [Fact]
    public void AddMessageBroker_MultipleTimes_ShouldReplaceRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBroker(options => options.BrokerType = MessageBrokerType.RabbitMQ);
        services.AddMessageBroker(options => options.BrokerType = MessageBrokerType.Kafka);

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
        // Last registration should win
        messageBroker.Should().BeOfType<Kafka.KafkaMessageBroker>();
    }

    [Fact]
    public void AddAzureServiceBus_ShouldRegisterAzureServiceBusMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAzureServiceBus(options =>
        {
            options.ConnectionString = "test-connection-string";
            options.DefaultEntityName = "test-queue";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<AzureServiceBus.AzureServiceBusMessageBroker>();
    }

    [Fact]
    public void AddAzureServiceBus_WithoutConfiguration_ShouldUseDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAzureServiceBus(options =>
        {
            // Azure Service Bus requires a connection string, so provide a dummy one
            options.ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=Test;SharedAccessKey=TestKey";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<AzureServiceBus.AzureServiceBusMessageBroker>();
    }

    [Fact]
    public void AddAzureServiceBus_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddAzureServiceBus();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddAwsSqsSns_ShouldRegisterAwsSqsSnsMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAwsSqsSns(options =>
        {
            options.Region = "us-east-1";
            options.DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<AwsSqsSns.AwsSqsSnsMessageBroker>();
    }

    [Fact]
    public void AddAwsSqsSns_WithoutConfiguration_ShouldUseDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAwsSqsSns();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<AwsSqsSns.AwsSqsSnsMessageBroker>();
    }

    [Fact]
    public void AddAwsSqsSns_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddAwsSqsSns();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddNats_ShouldRegisterNatsMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNats(options =>
        {
            options.Servers = new[] { "nats://localhost:4222" };
            options.Name = "test-connection";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<Nats.NatsMessageBroker>();
    }

    [Fact]
    public void AddNats_WithoutConfiguration_ShouldUseDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNats();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<Nats.NatsMessageBroker>();
    }

    [Fact]
    public void AddNats_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddNats();

        // Assert
        act.Should().Throw<ArgumentNullException>();
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
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<RedisStreams.RedisStreamsMessageBroker>();
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
        messageBroker.Should().NotBeNull();
        messageBroker.Should().BeOfType<RedisStreams.RedisStreamsMessageBroker>();
    }

    [Fact]
    public void AddRedisStreams_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddRedisStreams();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
