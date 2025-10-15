using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Extensions;

public class ServiceFactoryExtensionsTests
{
    [Fact]
    public void GetService_WithNullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ServiceFactory)null!).GetService<ITestService>());
    }

    [Fact]
    public void GetService_WithRegisteredService_ReturnsService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceType => serviceProvider.GetService(serviceType));

        // Act
        var result = factory.GetService<ITestService>();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestService>(result);
    }

    [Fact]
    public void GetService_WithUnregisteredService_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceType => serviceProvider.GetService(serviceType));

        // Act
        var result = factory.GetService<ITestService>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRequiredService_WithNullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ServiceFactory)null!).GetRequiredService<ITestService>());
    }

    [Fact]
    public void GetRequiredService_WithRegisteredService_ReturnsService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceType => serviceProvider.GetService(serviceType));

        // Act
        var result = factory.GetRequiredService<ITestService>();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestService>(result);
    }

    [Fact]
    public void GetRequiredService_WithUnregisteredService_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceType => serviceProvider.GetService(serviceType));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.GetRequiredService<ITestService>());
        Assert.Contains("Required service of type", exception.Message);
    }

    [Fact]
    public void GetServices_WithNullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ServiceFactory)null!).GetServices<ITestService>());
    }

    [Fact]
    public void GetServices_WithRegisteredServices_ReturnsServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        services.AddTransient<ITestService, TestService2>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceType => serviceProvider.GetService(serviceType));

        // Act
        var result = factory.GetServices<ITestService>().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s is TestService);
        Assert.Contains(result, s => s is TestService2);
    }

    [Fact]
    public void GetServices_WithNoRegisteredServices_ReturnsEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceType => serviceProvider.GetService(serviceType));

        // Act
        var result = factory.GetServices<ITestService>();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void TryGetService_WithNullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ServiceFactory)null!).TryGetService<ITestService>(out _));
    }

    [Fact]
    public void TryGetService_WithRegisteredService_ReturnsTrueAndService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceType => serviceProvider.GetService(serviceType));

        // Act
        var result = factory.TryGetService<ITestService>(out var service);

        // Assert
        Assert.True(result);
        Assert.NotNull(service);
        Assert.IsType<TestService>(service);
    }

    [Fact]
    public void TryGetService_WithUnregisteredService_ReturnsFalseAndNull()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceType => serviceProvider.GetService(serviceType));

        // Act
        var result = factory.TryGetService<ITestService>(out var service);

        // Assert
        Assert.False(result);
        Assert.Null(service);
    }

    [Fact]
    public void TryGetService_WithExceptionDuringResolution_ReturnsFalseAndNull()
    {
        // Arrange
        var factory = new ServiceFactory(_ => throw new Exception("Test exception"));

        // Act
        var result = factory.TryGetService<ITestService>(out var service);

        // Assert
        Assert.False(result);
        Assert.Null(service);
    }

    [Fact]
    public void CreateScopedFactory_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceFactoryExtensions.CreateScopedFactory(null!));
    }

    [Fact]
    public void CreateScopedFactory_WithValidServiceProvider_CreatesFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var factory = ServiceFactoryExtensions.CreateScopedFactory(serviceProvider);
        var service = factory.GetService<ITestService>();

        // Assert
        Assert.NotNull(factory);
        Assert.NotNull(service);
        Assert.IsType<TestService>(service);
    }

    // Test interfaces and classes
    public interface ITestService { }
    public class TestService : ITestService { }
    public class TestService2 : ITestService { }
}