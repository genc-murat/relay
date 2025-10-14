using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Extensions;

public class ServiceRegistrationHelperTests
{
    [Fact]
    public void ValidateServices_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServices(null!));
    }

    [Fact]
    public void ValidateServices_WithValidServices_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        ServiceRegistrationHelper.ValidateServices(services);
    }

    [Fact]
    public void ValidateServicesAndConfiguration_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndConfiguration<string>(null!, _ => { }));
    }

    [Fact]
    public void ValidateServicesAndConfiguration_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndConfiguration<string>(services, null!));
    }

    [Fact]
    public void ValidateServicesAndConfiguration_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        ServiceRegistrationHelper.ValidateServicesAndConfiguration<string>(services, _ => { });
    }

    [Fact]
    public void ValidateServicesAndFactory_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndFactory<string>(null!, _ => ""));
    }

    [Fact]
    public void ValidateServicesAndFactory_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndFactory<string>(services, null!));
    }

    [Fact]
    public void ValidateServicesAndFactory_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        ServiceRegistrationHelper.ValidateServicesAndFactory<string>(services, _ => "");
    }

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

    [Fact]
    public void TryAddEnumerable_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddEnumerable(null!, typeof(ITestService), typeof(TestService)));
    }

    [Fact]
    public void TryAddEnumerable_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddEnumerable(services, typeof(ITestService), typeof(TestService));

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddEnumerable_Generic_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddEnumerable<ITestService, TestService>(null!));
    }

    [Fact]
    public void TryAddEnumerable_Generic_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddEnumerable<ITestService, TestService>(services);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddEnumerable_Generic_WithLifetime_AddsServiceWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddEnumerable<ITestService, TestService>(services, ServiceLifetime.Singleton);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void ConfigureOptions_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ConfigureOptions<TestOptions>(null!, _ => { }));
    }

    [Fact]
    public void ConfigureOptions_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ConfigureOptions<TestOptions>(services, null!));
    }

    [Fact]
    public void ConfigureOptions_WithValidParameters_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.ConfigureOptions<TestOptions>(services, options => options.Value = "test");

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.Equal("test", options.Value.Value);
    }

    [Fact]
    public void ConfigureOptionsWithDefault_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ConfigureOptionsWithDefault<TestOptions>(null!));
    }

    [Fact]
    public void ConfigureOptionsWithDefault_WithNullConfigure_ConfiguresDefault()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.ConfigureOptionsWithDefault<TestOptions>(services, null);

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void ConfigureOptionsWithDefault_WithConfigure_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.ConfigureOptionsWithDefault<TestOptions>(services, options => options.Value = "configured");

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.Equal("configured", options.Value.Value);
    }

    [Fact]
    public void AddService_Generic_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.AddService<ITestService, TestService>(null!));
    }

    [Fact]
    public void AddService_Generic_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.AddService<ITestService, TestService>(services);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddService_Generic_WithLifetime_AddsServiceWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.AddService<ITestService, TestService>(services, ServiceLifetime.Singleton);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddService_WithFactory_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.AddService<ITestService>(null!, _ => new TestService()));
    }

    [Fact]
    public void AddService_WithFactory_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.AddService<ITestService>(services, null!));
    }

    [Fact]
    public void AddService_WithFactory_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.AddService<ITestService>(services, _ => new TestService());

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddService_WithFactory_WithLifetime_AddsServiceWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.AddService<ITestService>(services, _ => new TestService(), ServiceLifetime.Singleton);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddServiceInstance_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.AddServiceInstance<ITestService>(null!, new TestService()));
    }

    [Fact]
    public void AddServiceInstance_WithNullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.AddServiceInstance<ITestService>(services, null!));
    }

    [Fact]
    public void AddServiceInstance_WithValidParameters_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new TestService();

        // Act
        var result = ServiceRegistrationHelper.AddServiceInstance<ITestService>(services, instance);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Same(instance, descriptor.ImplementationInstance);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void DecorateService_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.DecorateService<ITestService, TestService>(null!));
    }

    [Fact]
    public void DecorateService_WithValidParameters_DecoratesService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        // Act
        var result = ServiceRegistrationHelper.DecorateService<ITestService, TestService>(services);

        // Assert
        Assert.Same(services, result);
        // Note: Decorate adds a new descriptor, so there should be 2 now
        Assert.Equal(2, services.Count);
    }

    [Fact]
    public void TryDecorateService_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryDecorateService<ITestService, TestService>(null!));
    }

    [Fact]
    public void TryDecorateService_WithNoExistingService_DoesNotDecorate()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryDecorateService<ITestService, TestService>(services);

        // Assert
        Assert.Same(services, result);
        Assert.Empty(services);
    }

    [Fact]
    public void TryDecorateService_WithExistingService_DecoratesService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        // Act
        var result = ServiceRegistrationHelper.TryDecorateService<ITestService, TestService>(services);

        // Assert
        Assert.Same(services, result);
        Assert.Equal(2, services.Count);
    }

    [Fact]
    public void AddMultipleServices_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.AddMultipleServices(null!, new Dictionary<Type, (Type, ServiceLifetime)>()));
    }

    [Fact]
    public void AddMultipleServices_WithNullRegistrations_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.AddMultipleServices(services, null!));
    }

    [Fact]
    public void AddMultipleServices_WithValidParameters_AddsServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var registrations = new Dictionary<Type, (Type Implementation, ServiceLifetime Lifetime)>
        {
            [typeof(ITestService)] = (typeof(TestService), ServiceLifetime.Singleton),
            [typeof(ITestService2)] = (typeof(TestService2), ServiceLifetime.Transient)
        };

        // Act
        var result = ServiceRegistrationHelper.AddMultipleServices(services, registrations);

        // Assert
        Assert.Same(services, result);
        Assert.Equal(2, services.Count);
        var singletonDescriptor = services.First(d => d.Lifetime == ServiceLifetime.Singleton);
        var transientDescriptor = services.First(d => d.Lifetime == ServiceLifetime.Transient);
        Assert.Equal(typeof(ITestService), singletonDescriptor.ServiceType);
        Assert.Equal(typeof(TestService), singletonDescriptor.ImplementationType);
        Assert.Equal(typeof(ITestService2), transientDescriptor.ServiceType);
        Assert.Equal(typeof(TestService2), transientDescriptor.ImplementationType);
    }

    [Fact]
    public void TryAddConditional_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddConditional<ITestService, TestService>(null!, _ => true));
    }

    [Fact]
    public void TryAddConditional_WithNullCondition_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddConditional<ITestService, TestService>(services, null!));
    }

    [Fact]
    public void TryAddConditional_WithConditionTrue_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddConditional<ITestService, TestService>(services, _ => true);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddConditional_WithConditionFalse_DoesNotAddService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddConditional<ITestService, TestService>(services, _ => false);

        // Assert
        Assert.Same(services, result);
        Assert.Empty(services);
    }

    [Fact]
    public void TryAddConditional_WithLifetime_AddsServiceWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddConditional<ITestService, TestService>(services, _ => true, ServiceLifetime.Singleton);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
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