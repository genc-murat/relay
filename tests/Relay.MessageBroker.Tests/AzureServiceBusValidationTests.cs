using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AzureServiceBusValidationTests
{
    [Fact]
    public void AzureServiceBusOptions_WithInvalidConnectionString_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddAzureServiceBus(options =>
            {
                options.ConnectionString = ""; // Empty connection string
            }));

        Assert.Contains("ConnectionString", exception.Message);
    }

    [Fact]
    public void AzureServiceBusOptions_WithInvalidSubscriptionNameForTopic_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddAzureServiceBus(options =>
            {
                options.ConnectionString = "test-connection-string"; // Valid connection string
                options.EntityType = AzureEntityType.Topic;
                options.SubscriptionName = ""; // Empty subscription name for topic
            }));

        Assert.Contains("SubscriptionName", exception.Message);
    }
}