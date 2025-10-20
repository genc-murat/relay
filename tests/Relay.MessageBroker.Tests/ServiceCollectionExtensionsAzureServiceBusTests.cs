using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsAzureServiceBusTests
{
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
}