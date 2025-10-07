extern alias RelayCore;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.SourceGenerator.Tests;

public class DIIntegrationTests
{
    [Fact]
    public void ServiceCollection_CanRegisterRelayServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - This should compile without errors
        // In a real scenario, this would use the generated AddRelay extension method
        services.AddSingleton<RelayCore::Relay.Core.Contracts.Core.IRelay, RelayCore::Relay.Core.Implementation.Core.RelayImplementation>();

        var serviceProvider = services.BuildServiceProvider();
        var relay = serviceProvider.GetService<RelayCore::Relay.Core.Contracts.Core.IRelay>();

        Assert.NotNull(relay);
        Assert.IsType<RelayCore::Relay.Core.Implementation.Core.RelayImplementation>(relay);
    }

    [Fact]
    public void RelayImplementation_CanBeConstructed()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayCore::Relay.Core.Implementation.Core.RelayImplementation(serviceProvider);

        // Assert
        Assert.NotNull(relay);
    }

    [Fact]
    public void RelayImplementation_ThrowsForNullServiceProvider()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayCore::Relay.Core.Implementation.Core.RelayImplementation(null!));
    }
}