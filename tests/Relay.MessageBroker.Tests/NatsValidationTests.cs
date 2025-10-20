using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class NatsValidationTests
{
    [Fact]
    public void NatsOptions_WithInvalidServers_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddNats(options =>
            {
                options.Servers = Array.Empty<string>(); // Empty servers array
            }));

        Assert.Contains("Servers", exception.Message);
    }
}