using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsAwsSqsSnsTests
{
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
    public void AddAwsSqsSns_WithNullRegion_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddAwsSqsSns(options =>
            {
                options.Region = null!;
            }));
    }

    [Fact]
    public void AddAwsSqsSns_WithEmptyRegion_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddAwsSqsSns(options =>
            {
                options.Region = "";
            }));
    }
}