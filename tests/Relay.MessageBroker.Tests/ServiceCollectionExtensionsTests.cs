using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Relay.Core.ContractValidation;
using Relay.Core;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsTests
{
    private class TestContractValidator : IContractValidator
    {
        public ValueTask<IEnumerable<string>> ValidateRequestAsync(object request, JsonSchemaContract schema, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Enumerable.Empty<string>());
        }

        public ValueTask<IEnumerable<string>> ValidateResponseAsync(object response, JsonSchemaContract schema, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Enumerable.Empty<string>());
        }
    }

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
        Assert.NotNull(messageBroker);
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
        Assert.NotNull(messageBroker);
        Assert.IsType<RabbitMQ.RabbitMQMessageBroker>(messageBroker);
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
        Assert.NotNull(messageBroker);
        Assert.IsType<Kafka.KafkaMessageBroker>(messageBroker);
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
        Assert.Contains(hostedServices, s => s is MessageBrokerHostedService);
    }

    [Fact]
    public void AddMessageBroker_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddMessageBroker(_ => { }));
    }

    [Fact]
    public void AddMessageBroker_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddMessageBroker(null!));
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
    public void AddKafka_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddKafka());
    }

    [Fact]
    public void AddMessageBrokerHostedService_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddMessageBrokerHostedService());
    }

    [Fact]
    public void AddMessageBroker_WithUnsupportedBrokerType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = (MessageBrokerType)999; // Invalid broker type
            }));
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
        Assert.NotNull(messageBroker);
        Assert.IsType<Kafka.KafkaMessageBroker>(messageBroker);
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
        Assert.NotNull(messageBroker);
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
        Assert.NotNull(messageBroker);
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
        Assert.NotNull(messageBroker);
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
        Assert.NotNull(messageBroker);
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
        Assert.NotNull(messageBroker);
        // Last registration should win
        Assert.IsType<Kafka.KafkaMessageBroker>(messageBroker);
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
        Assert.NotNull(messageBroker);
        Assert.IsType<AzureServiceBus.AzureServiceBusMessageBroker>(messageBroker);
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
        Assert.NotNull(messageBroker);
        Assert.IsType<AzureServiceBus.AzureServiceBusMessageBroker>(messageBroker);
    }

    [Fact]
    public void AddAzureServiceBus_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddAzureServiceBus());
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
        Assert.NotNull(messageBroker);
        Assert.IsType<AwsSqsSns.AwsSqsSnsMessageBroker>(messageBroker);
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
        Assert.NotNull(messageBroker);
        Assert.IsType<AwsSqsSns.AwsSqsSnsMessageBroker>(messageBroker);
    }

    [Fact]
    public void AddAwsSqsSns_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddAwsSqsSns());
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
        Assert.NotNull(messageBroker);
        Assert.IsType<Nats.NatsMessageBroker>(messageBroker);
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
        Assert.NotNull(messageBroker);
        Assert.IsType<Nats.NatsMessageBroker>(messageBroker);
    }

    [Fact]
    public void AddNats_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddNats());
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
    public void AddMessageBroker_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var chainedServices = services.AddRabbitMQ().AddMessageBrokerHostedService();

        // Assert
        Assert.Same(services, chainedServices);
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
    public void AddKafka_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafka(options =>
        {
            options.BootstrapServers = "testserver:9092";
            options.ConsumerGroupId = "test-group";
        });

        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MessageBrokerOptions>>();

        // Assert
        Assert.Equal(MessageBrokerType.Kafka, configuredOptions.Value.BrokerType);
        Assert.NotNull(configuredOptions.Value.Kafka);
        Assert.Equal("testserver:9092", configuredOptions.Value.Kafka.BootstrapServers);
        Assert.Equal("test-group", configuredOptions.Value.Kafka.ConsumerGroupId);
    }

    [Fact]
    public void AddAzureServiceBus_ShouldConfigureOptions()
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
        var configuredOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MessageBrokerOptions>>();

        // Assert
        Assert.Equal(MessageBrokerType.AzureServiceBus, configuredOptions.Value.BrokerType);
        Assert.NotNull(configuredOptions.Value.AzureServiceBus);
        Assert.Equal("test-connection-string", configuredOptions.Value.AzureServiceBus.ConnectionString);
        Assert.Equal("test-queue", configuredOptions.Value.AzureServiceBus.DefaultEntityName);
    }

    [Fact]
    public void AddAwsSqsSns_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAwsSqsSns(options =>
        {
            options.Region = "us-west-2";
            options.DefaultQueueUrl = "https://sqs.us-west-2.amazonaws.com/123456789012/test-queue";
        });

        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MessageBrokerOptions>>();

        // Assert
        Assert.Equal(MessageBrokerType.AwsSqsSns, configuredOptions.Value.BrokerType);
        Assert.NotNull(configuredOptions.Value.AwsSqsSns);
        Assert.Equal("us-west-2", configuredOptions.Value.AwsSqsSns.Region);
        Assert.Equal("https://sqs.us-west-2.amazonaws.com/123456789012/test-queue", configuredOptions.Value.AwsSqsSns.DefaultQueueUrl);
    }

    [Fact]
    public void AddNats_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNats(options =>
        {
            options.Servers = new[] { "nats://testserver:4222" };
            options.Name = "test-connection";
        });

        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MessageBrokerOptions>>();

        // Assert
        Assert.Equal(MessageBrokerType.Nats, configuredOptions.Value.BrokerType);
        Assert.NotNull(configuredOptions.Value.Nats);
        Assert.Equal(new[] { "nats://testserver:4222" }, configuredOptions.Value.Nats.Servers);
        Assert.Equal("test-connection", configuredOptions.Value.Nats.Name);
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
    public void AddMessageBroker_WithContractValidator_ShouldPassValidatorToBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContractValidator, TestContractValidator>();

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
        Assert.NotNull(messageBroker);
    }
}
