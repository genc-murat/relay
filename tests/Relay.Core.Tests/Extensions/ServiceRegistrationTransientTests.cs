using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Extensions;

public class ServiceRegistrationTransientTests
{
    [Fact]
    public void TryAddTransient_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddTransient(null!, typeof(ITestService), typeof(TestService)));
    }

    [Fact]
    public void TryAddTransient_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddTransient(services, typeof(ITestService), typeof(TestService));

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddTransient_Generic_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddTransient<ITestService, TestService>(null!));
    }

    [Fact]
    public void TryAddTransient_Generic_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddTransient<ITestService, TestService>(services);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddTransient_WithFactory_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddTransient<ITestService>(null!, _ => new TestService()));
    }

    [Fact]
    public void TryAddTransient_WithFactory_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddTransient<ITestService>(services, _ => new TestService());

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
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
