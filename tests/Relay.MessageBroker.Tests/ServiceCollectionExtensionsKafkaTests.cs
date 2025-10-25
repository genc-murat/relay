using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsKafkaTests
{
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
    public void AddKafka_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddKafka());
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
    public void AddKafka_WithNullBootstrapServers_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddKafka(options =>
            {
                options.BootstrapServers = null!;
            }));
    }

    [Fact]
    public void AddKafka_WithEmptyBootstrapServers_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddKafka(options =>
            {
                options.BootstrapServers = "";
            }));
    }

    [Fact]
    public void AddKafka_WithNullConsumerGroupId_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddKafka(options =>
            {
                options.BootstrapServers = "localhost:9092";
                options.ConsumerGroupId = null!;
            }));
    }

    [Fact]
    public void AddKafka_WithEmptyConsumerGroupId_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddKafka(options =>
            {
                options.BootstrapServers = "localhost:9092";
                options.ConsumerGroupId = "";
            }));
    }
}