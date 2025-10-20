using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AwsSqsSnsValidationTests
{
    [Fact]
    public void AwsSqsSnsOptions_WithInvalidRegion_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddAwsSqsSns(options =>
            {
                options.Region = ""; // Empty region
            }));

        Assert.Contains("Region", exception.Message);
    }
}