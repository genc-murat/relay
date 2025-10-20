using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class KafkaValidationTests
{
    [Fact]
    public void KafkaOptions_WithInvalidBootstrapServers_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddKafka(options =>
            {
                options.BootstrapServers = ""; // Empty bootstrap servers
            }));

        Assert.Contains("BootstrapServers", exception.Message);
    }

    [Fact]
    public void KafkaOptions_WithInvalidConsumerGroupId_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddKafka(options =>
            {
                options.ConsumerGroupId = ""; // Empty consumer group id
            }));

        Assert.Contains("ConsumerGroupId", exception.Message);
    }
}