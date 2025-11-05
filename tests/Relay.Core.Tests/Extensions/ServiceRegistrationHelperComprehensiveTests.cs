using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.Extensions;

public class ServiceRegistrationHelperComprehensiveTests
{
    [Fact]
    public void ValidateServices_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServices(null!));
    }

    [Fact]
    public void ValidateServices_WithValidServiceCollection_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should not throw
        ServiceRegistrationHelper.ValidateServices(services);
    }

    [Fact]
    public void ValidateServicesAndConfiguration_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        Action<object> configure = _ => { };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndConfiguration<object>(null!, configure));
    }

    [Fact]
    public void ValidateServicesAndConfiguration_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndConfiguration<object>(services, null!));
    }

    [Fact]
    public void ValidateServicesAndConfiguration_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<object> configure = _ => { };

        // Act & Assert - Should not throw
        ServiceRegistrationHelper.ValidateServicesAndConfiguration<object>(services, configure);
    }

    [Fact]
    public void ValidateServicesAndFactory_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        Func<object, object> factory = _ => new object();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndFactory<object>(null!, factory));
    }

    [Fact]
    public void ValidateServicesAndFactory_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndFactory<object>(services, null!));
    }

    [Fact]
    public void ValidateServicesAndFactory_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<object, object> factory = _ => new object();

        // Act & Assert - Should not throw
        ServiceRegistrationHelper.ValidateServicesAndFactory<object>(services, factory);
    }

    [Fact]
    public void TryAddSingleton_WithTypeAndImplementation_AddsServiceIfNotExists()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceType = typeof(ITestService);
        var implementationType = typeof(TestServiceImplementation);

        // Act
        var result = ServiceRegistrationHelper.TryAddSingleton(services, serviceType, implementationType);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(serviceType, descriptor.ServiceType);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddSingleton_WithTypeAndImplementation_DoesNotAddIfAlreadyExists()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestServiceImplementation>();
        var serviceType = typeof(ITestService);
        var implementationType = typeof(AnotherTestServiceImplementation);

        // Act
        var result = ServiceRegistrationHelper.TryAddSingleton(services, serviceType, implementationType);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestServiceImplementation), descriptor.ImplementationType);
    }

    [Fact]
    public void TryAddSingleton_Generic_AddsServiceIfNotExists()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddSingleton<ITestService, TestServiceImplementation>(services);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestServiceImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddSingleton_WithFactory_AddsServiceIfNotExists()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, ITestService> factory = _ => new TestServiceImplementation();

        // Act
        var result = ServiceRegistrationHelper.TryAddSingleton(services, factory);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddTransient_WithTypeAndImplementation_AddsServiceIfNotExists()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceType = typeof(ITestService);
        var implementationType = typeof(TestServiceImplementation);

        // Act
        var result = ServiceRegistrationHelper.TryAddTransient(services, serviceType, implementationType);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(serviceType, descriptor.ServiceType);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddTransient_WithTypeAndImplementation_DoesNotAddIfAlreadyExists()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestServiceImplementation>();
        var serviceType = typeof(ITestService);
        var implementationType = typeof(AnotherTestServiceImplementation);

        // Act
        var result = ServiceRegistrationHelper.TryAddTransient(services, serviceType, implementationType);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestServiceImplementation), descriptor.ImplementationType);
    }

    [Fact]
    public void TryAddTransient_Generic_AddsServiceIfNotExists()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddTransient<ITestService, TestServiceImplementation>(services);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestServiceImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddTransient_WithFactory_AddsServiceIfNotExists()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, ITestService> factory = _ => new TestServiceImplementation();

        // Act
        var result = ServiceRegistrationHelper.TryAddTransient(services, factory);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddTransient_WithTypeAndImplementation_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceType = typeof(ITestService);
        var implementationType = typeof(TestServiceImplementation);

        // Act
        var result = ServiceRegistrationHelper.AddTransient(services, serviceType, implementationType);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(serviceType, descriptor.ServiceType);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddTransient_Generic_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.AddTransient<ITestService, TestServiceImplementation>(services);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestServiceImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddEnumerable_WithTypeAndImplementation_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceType = typeof(ITestService);
        var implementationType = typeof(TestServiceImplementation);

        // Act
        var result = ServiceRegistrationHelper.TryAddEnumerable(services, serviceType, implementationType);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(serviceType, descriptor.ServiceType);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddEnumerable_WithLifetime_AddsServiceWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryAddEnumerable<ITestService, TestServiceImplementation>(services, ServiceLifetime.Singleton);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestServiceImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void ConfigureOptions_WithConfiguration_AddsOptionsService()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<TestOptions> configure = options => options.Value = "configured";

        // Act
        var result = ServiceRegistrationHelper.ConfigureOptions(services, configure);

        // Assert
        Assert.Same(services, result);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IConfigureOptions<TestOptions>));
    }

    [Fact]
    public void ConfigureOptionsWithDefault_WithConfiguration_AddsConfiguredOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<TestOptions> configure = options => options.Value = "configured";

        // Act
        var result = ServiceRegistrationHelper.ConfigureOptionsWithDefault<TestOptions>(services, configure);

        // Assert
        Assert.Same(services, result);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IConfigureOptions<TestOptions>));
    }

    [Fact]
    public void ConfigureOptionsWithDefault_WithoutConfiguration_AddsDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.ConfigureOptionsWithDefault<TestOptions>(services);

        // Assert
        Assert.Same(services, result);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IConfigureOptions<TestOptions>));
    }

    [Fact]
    public void AddService_GenericWithLifetime_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.AddService<ITestService, TestServiceImplementation>(services, ServiceLifetime.Scoped);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestServiceImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddService_WithFactory_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, ITestService> factory = _ => new TestServiceImplementation();

        // Act
        var result = ServiceRegistrationHelper.AddService(services, factory, ServiceLifetime.Scoped);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddServiceInstance_WithInstance_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new TestServiceImplementation();

        // Act
        var result = ServiceRegistrationHelper.AddServiceInstance(services, instance);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(TestServiceImplementation), descriptor.ServiceType);
        Assert.Same(instance, descriptor.ImplementationInstance);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
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
    public void DecorateService_AddsDecoration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestServiceImplementation>();

        // Act
        var result = ServiceRegistrationHelper.DecorateService<ITestService, TestServiceDecorator>(services);

        // Assert
        Assert.Same(services, result);
        Assert.Equal(2, services.Count);
    }

    [Fact]
    public void TryDecorateService_WhenServiceExists_AddsDecoration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestServiceImplementation>();

        // Act
        var result = ServiceRegistrationHelper.TryDecorateService<ITestService, TestServiceDecorator>(services);

        // Assert
        Assert.Same(services, result);
        Assert.Equal(2, services.Count);
    }

    [Fact]
    public void TryDecorateService_WhenServiceDoesNotExist_DoesNotAddDecoration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.TryDecorateService<ITestService, TestServiceDecorator>(services);

        // Assert
        Assert.Same(services, result);
        Assert.Empty(services);
    }

    [Fact]
    public void AddMultipleServices_WithRegistrations_AddsAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var registrations = new Dictionary<Type, (Type Implementation, ServiceLifetime Lifetime)>
        {
            { typeof(ITestService), (typeof(TestServiceImplementation), ServiceLifetime.Singleton) },
            { typeof(IAnotherService), (typeof(AnotherServiceImplementation), ServiceLifetime.Transient) }
        };

        // Act
        var result = ServiceRegistrationHelper.AddMultipleServices(services, registrations);

        // Assert
        Assert.Same(services, result);
        Assert.Equal(2, services.Count);
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
    public void TryAddConditional_WhenConditionIsTrue_AddsService()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceCollection, bool> condition = _ => true;

        // Act
        var result = ServiceRegistrationHelper.TryAddConditional<ITestService, TestServiceImplementation>(services, condition);

        // Assert
        Assert.Same(services, result);
        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestServiceImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void TryAddConditional_WhenConditionIsFalse_DoesNotAddService()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceCollection, bool> condition = _ => false;

        // Act
        var result = ServiceRegistrationHelper.TryAddConditional<ITestService, TestServiceImplementation>(services, condition);

        // Assert
        Assert.Same(services, result);
        Assert.Empty(services);
    }

    [Fact]
    public void TryAddConditional_WithNullCondition_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.TryAddConditional<ITestService, TestServiceImplementation>(services, null!));
    }

    // Test interfaces and classes
    public interface ITestService
    {
        string GetValue();
    }

    public interface IAnotherService
    {
        string GetAnotherValue();
    }

    public class TestServiceImplementation : ITestService
    {
        public string GetValue() => "Implementation";
    }

    public class AnotherTestServiceImplementation : ITestService
    {
        public string GetValue() => "Another Implementation";
    }

    public class TestServiceDecorator : ITestService
    {
        private readonly ITestService _testService;

        public TestServiceDecorator(ITestService testService)
        {
            _testService = testService;
        }

        public string GetValue() => "Decorated: " + _testService.GetValue();
    }

    public class AnotherServiceImplementation : IAnotherService
    {
        public string GetAnotherValue() => "Another Service";
    }

    public class TestOptions
    {
        public string Value { get; set; } = string.Empty;
        
        public TestOptions()
        {
        }
    }
}
