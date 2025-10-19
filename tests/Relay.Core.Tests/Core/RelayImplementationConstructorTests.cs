using System;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests.Core;

public class RelayImplementationConstructorTests
{
    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // Assert
        Assert.NotNull(relay);
        Assert.IsAssignableFrom<IRelay>(relay);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayImplementation(null!));
    }

    [Fact]
    public void ServiceFactory_Property_ShouldReturnConfiguredFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        ServiceFactory expectedFactory = type => serviceProvider.GetService(type);
        var relay = new RelayImplementation(serviceProvider, expectedFactory);

        // Act
        var actualFactory = relay.ServiceFactory;

        // Assert
        Assert.Equal(expectedFactory, actualFactory);
    }

    [Fact]
    public void ServiceFactory_Property_ShouldReturnDefaultFactory_WhenNotProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act
        var factory = relay.ServiceFactory;

        // Assert
        Assert.NotNull(factory);
        // The factory should be the serviceProvider.GetService method
        Assert.Equal(serviceProvider.GetService(typeof(string)), factory(typeof(string)));
    }

    [Fact]
    public void Constructor_WithServiceFactory_UsesProvidedFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        ServiceFactory factory = type => serviceProvider.GetService(type);

        // Act
        var relay = new RelayImplementation(serviceProvider, factory);

        // Assert
        Assert.NotNull(relay);
    }

    [Fact]
    public void Constructor_WithNullServiceFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RelayImplementation(serviceProvider, null!));
    }
}