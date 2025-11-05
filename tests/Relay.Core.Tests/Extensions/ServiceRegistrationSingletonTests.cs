using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Extensions;

public class ServiceRegistrationSingletonTests
{
    [Fact]
    public void TryAddSingleton_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddSingleton(null!, typeof(string), typeof(string)));
    }

    [Fact]
    public void TryAddSingleton_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddSingleton(services, typeof(ITestService), typeof(TestService));

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddSingleton_Generic_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddSingleton<ITestService, TestService>(null!));
    }

    [Fact]
    public void TryAddSingleton_Generic_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddSingleton<ITestService, TestService>(services);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddSingleton_Generic_DoesNotAddIfAlreadyRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestService>();

        // Act
        ServiceRegistrationHelper.TryAddSingleton<ITestService, TestService>(services);

        // Assert
        Assert.Single(services);
    }

    [Fact]
    public void TryAddSingleton_WithFactory_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddSingleton<ITestService>(null!, _ => new TestService()));
    }

    [Fact]
    public void TryAddSingleton_WithFactory_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddSingleton<ITestService>(services, null!));
    }

    [Fact]
    public void TryAddSingleton_WithFactory_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddSingleton<ITestService>(services, _ => new TestService());

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    // Test interfaces and classes
    public interface ITestService { }
    public class TestService : ITestService { }
    public interface ITestService2 { }
    public class TestService2 : ITestService2 { }

    public class TestOptions
    {
        public string? Value { get; set; }
    }
}
