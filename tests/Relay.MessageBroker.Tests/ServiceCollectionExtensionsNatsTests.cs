using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsNatsTests
{
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
    public void AddNats_WithNullServers_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddNats(options =>
            {
                options.Servers = null!;
            }));
    }

    [Fact]
    public void AddNats_WithEmptyServers_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddNats(options =>
            {
                options.Servers = Array.Empty<string>();
            }));
    }
}