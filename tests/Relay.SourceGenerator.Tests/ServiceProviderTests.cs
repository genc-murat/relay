using Xunit;
using Relay.SourceGenerator.Core;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for SimpleServiceProvider implementation.
/// </summary>
public class ServiceProviderTests
{
    private interface ITestService
    {
        string GetValue();
    }

    private class TestService : ITestService
    {
        public string GetValue() => "test";
    }

    private class AnotherTestService : ITestService
    {
        public string GetValue() => "another";
    }

    [Fact]
    public void RegisterService_WithInstance_StoresService()
    {
        // Arrange
        var provider = new SimpleServiceProvider();
        var service = new TestService();

        // Act
        provider.RegisterService<ITestService>(service);
        var retrieved = provider.GetService<ITestService>();

        // Assert
        Assert.Same(service, retrieved);
    }

    [Fact]
    public void RegisterService_WithFactory_CreatesServiceOnDemand()
    {
        // Arrange
        var provider = new SimpleServiceProvider();
        provider.RegisterService<ITestService>(sp => new TestService());

        // Act
        var service = provider.GetService<ITestService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<TestService>(service);
    }

    [Fact]
    public void RegisterService_WithFactory_CachesInstance()
    {
        // Arrange
        var provider = new SimpleServiceProvider();
        provider.RegisterService<ITestService>(sp => new TestService());

        // Act
        var service1 = provider.GetService<ITestService>();
        var service2 = provider.GetService<ITestService>();

        // Assert
        Assert.Same(service1, service2);
    }

    [Fact]
    public void GetService_WhenNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = new SimpleServiceProvider();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.GetService<ITestService>());
    }

    [Fact]
    public void GetServiceOrNull_WhenNotRegistered_ReturnsNull()
    {
        // Arrange
        var provider = new SimpleServiceProvider();

        // Act
        var service = provider.GetServiceOrNull<ITestService>();

        // Assert
        Assert.Null(service);
    }

    [Fact]
    public void GetServiceOrNull_WhenRegistered_ReturnsService()
    {
        // Arrange
        var provider = new SimpleServiceProvider();
        var service = new TestService();
        provider.RegisterService<ITestService>(service);

        // Act
        var retrieved = provider.GetServiceOrNull<ITestService>();

        // Assert
        Assert.Same(service, retrieved);
    }

    [Fact]
    public void RegisterService_OverwritesPreviousRegistration()
    {
        // Arrange
        var provider = new SimpleServiceProvider();
        var service1 = new TestService();
        var service2 = new AnotherTestService();

        // Act
        provider.RegisterService<ITestService>(service1);
        provider.RegisterService<ITestService>(service2);
        var retrieved = provider.GetService<ITestService>();

        // Assert
        Assert.Same(service2, retrieved);
    }

    [Fact]
    public void Clear_RemovesAllServices()
    {
        // Arrange
        var provider = new SimpleServiceProvider();
        provider.RegisterService<ITestService>(new TestService());

        // Act
        provider.Clear();

        // Assert
        Assert.Throws<InvalidOperationException>(() => provider.GetService<ITestService>());
    }

    [Fact]
    public void RegisterService_WithNullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new SimpleServiceProvider();
        ITestService nullService = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider.RegisterService(nullService));
    }

    [Fact]
    public void RegisterService_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new SimpleServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            provider.RegisterService<ITestService>((Func<Relay.SourceGenerator.Core.IServiceProvider, ITestService>)null!));
    }

    [Fact]
    public void Factory_CanAccessServiceProvider()
    {
        // Arrange
        var provider = new SimpleServiceProvider();
        Relay.SourceGenerator.Core.IServiceProvider? capturedProvider = null;

        provider.RegisterService<ITestService>(sp =>
        {
            capturedProvider = sp;
            return new TestService();
        });

        // Act
        provider.GetService<ITestService>();

        // Assert
        Assert.Same(provider, capturedProvider);
    }

    [Fact]
    public void MultipleServices_CanBeRegisteredIndependently()
    {
        // Arrange
        var provider = new SimpleServiceProvider();
        var testService = new TestService();
        var anotherService = new AnotherTestService();

        // Act
        provider.RegisterService<TestService>(testService);
        provider.RegisterService<AnotherTestService>(anotherService);

        // Assert
        Assert.Same(testService, provider.GetService<TestService>());
        Assert.Same(anotherService, provider.GetService<AnotherTestService>());
    }
}
