using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Extensions;

public class BaseServiceCollectionExtensionsTests
{
    [Fact]
    public void RegisterCoreServices_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).RegisterCoreServices(_ => { }));
    }

    [Fact]
    public void RegisterCoreServices_WithNullCoreRegistrations_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterCoreServices(null!));
    }

    [Fact]
    public void RegisterCoreServices_WithValidParameters_ExecutesRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var registered = false;

        // Act
        var result = services.RegisterCoreServices(s =>
        {
            registered = true;
            s.AddTransient<ITestService, TestService>();
        });

        // Assert
        Assert.Same(services, result);
        Assert.True(registered);
        Assert.Single(services);
    }

    [Fact]
    public void RegisterWithConfiguration_Generic_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).RegisterWithConfiguration<TestOptions>(_ => { }));
    }

    [Fact]
    public void RegisterWithConfiguration_Generic_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterWithConfiguration<TestOptions>(null!));
    }

    [Fact]
    public void RegisterWithConfiguration_Generic_WithValidParameters_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.RegisterWithConfiguration<TestOptions>(options => options.Value = "test");

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.Equal("test", options.Value.Value);
    }

    [Fact]
    public void RegisterWithConfiguration_Generic_WithServiceRegistrations_ExecutesRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var registered = false;

        // Act
        var result = services.RegisterWithConfiguration<TestOptions>(
            options => options.Value = "test",
            s => { registered = true; s.AddTransient<ITestService, TestService>(); });

        // Assert
        Assert.Same(services, result);
        Assert.True(registered);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.Equal("test", options.Value.Value);
        Assert.NotNull(serviceProvider.GetService<ITestService>());
    }

    [Fact]
    public void RegisterWithConfiguration_WithConfiguration_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).RegisterWithConfiguration<TestOptions>(configuration, "Test"));
    }

    [Fact]
    public void RegisterWithConfiguration_WithConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterWithConfiguration<TestOptions>(null!, "Test"));
    }

    [Fact]
    public void RegisterWithConfiguration_WithConfiguration_WithNullSectionName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.RegisterWithConfiguration<TestOptions>(configuration, null!));
    }

    [Fact]
    public void RegisterWithConfiguration_WithConfiguration_WithEmptySectionName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.RegisterWithConfiguration<TestOptions>(configuration, ""));
    }

    [Fact]
    public void RegisterWithConfiguration_WithConfiguration_WithValidParameters_BindsConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:Value"] = "configured"
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        var result = services.RegisterWithConfiguration<TestOptions>(configuration, "Test");

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.Equal("configured", options.Value.Value);
    }

    [Fact]
    public void RegisterWithConfiguration_WithConfiguration_WithPostConfigure_AppliesPostConfigure()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:Value"] = "configured"
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        var result = services.RegisterWithConfiguration<TestOptions>(
            configuration,
            "Test",
            postConfigure: options => options.Value = "postconfigured");

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.Equal("postconfigured", options.Value.Value);
    }

    [Fact]
    public void RegisterPipelineBehaviors_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).RegisterPipelineBehaviors(new Dictionary<Type, Type>()));
    }

    [Fact]
    public void RegisterPipelineBehaviors_WithNullBehaviors_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterPipelineBehaviors(null!));
    }

    [Fact]
    public void RegisterPipelineBehaviors_WithValidParameters_RegistersBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        var behaviors = new Dictionary<Type, Type>
        {
            [typeof(IPipelineBehavior)] = typeof(TestPipelineBehavior)
        };

        // Act
        var result = services.RegisterPipelineBehaviors(behaviors);

        // Assert
        Assert.Same(services, result);
        Assert.Single(services);
        var descriptor = services.Single();
        Assert.Equal(typeof(IPipelineBehavior), descriptor.ServiceType);
        Assert.Equal(typeof(TestPipelineBehavior), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void RegisterPipelineBehaviors_WithLifetime_RegistersWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var behaviors = new Dictionary<Type, Type>
        {
            [typeof(IPipelineBehavior)] = typeof(TestPipelineBehavior)
        };

        // Act
        var result = services.RegisterPipelineBehaviors(behaviors, ServiceLifetime.Singleton);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void RegisterServiceWithOptions_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).RegisterServiceWithOptions<ITestService>());
    }

    [Fact]
    public void RegisterServiceWithOptions_WithNoOptions_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.RegisterServiceWithOptions<ITestService>());
    }

    [Fact]
    public void RegisterServiceWithOptions_WithInstance_RegistersInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new TestService();

        // Act
        var result = services.RegisterServiceWithOptions<ITestService>(instance: instance);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Same(instance, descriptor.ImplementationInstance);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void RegisterServiceWithOptions_WithFactory_RegistersFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.RegisterServiceWithOptions<ITestService>(factory: _ => new TestService());

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void RegisterServiceWithOptions_WithImplementationType_RegistersType()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.RegisterServiceWithOptions<ITestService>(implementationType: typeof(TestService));

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void RegisterServiceWithOptions_WithInvalidImplementationType_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.RegisterServiceWithOptions<ITestService>(implementationType: typeof(string)));
    }

    [Fact]
    public void RegisterServiceWithOptions_WithLifetime_RegistersWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.RegisterServiceWithOptions<ITestService>(
            factory: _ => new TestService(),
            lifetime: ServiceLifetime.Transient);

        // Assert
        Assert.Same(services, result);
        var descriptor = services.Single();
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void RegisterWithDecorators_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).RegisterWithDecorators<ITestService>(typeof(TestService)));
    }

    [Fact]
    public void RegisterWithDecorators_WithNullDecoratorType_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterWithDecorators<ITestService>(null!));
    }

    [Fact]
    public void RegisterWithDecorators_WithValidParameters_DecoratesService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        // Act
        var result = services.RegisterWithDecorators<ITestService>(typeof(TestService));

        // Assert
        Assert.Same(services, result);
        Assert.Equal(2, services.Count);
    }

    [Fact]
    public void RegisterHealthChecks_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).RegisterHealthChecks(typeof(TestService)));
    }

    [Fact]
    public void RegisterHealthChecks_WithNullHealthCheckTypes_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterHealthChecks(null!));
    }

    [Fact]
    public void RegisterHealthChecks_WithValidParameters_RegistersHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.RegisterHealthChecks(typeof(TestService), typeof(TestService2));

        // Assert
        Assert.Same(services, result);
        Assert.Equal(2, services.Count);
        foreach (var descriptor in services)
        {
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
            Assert.Equal(descriptor.ServiceType, descriptor.ImplementationType);
        }
    }

    [Fact]
    public void RegisterFromAssembly_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).RegisterFromAssembly(typeof(BaseServiceCollectionExtensionsTests).Assembly, _ => true));
    }

    [Fact]
    public void RegisterFromAssembly_WithNullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterFromAssembly(null!, _ => true));
    }

    [Fact]
    public void RegisterFromAssembly_WithNullInterfaceFilter_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterFromAssembly(typeof(BaseServiceCollectionExtensionsTests).Assembly, null!));
    }

    [Fact]
    public void RegisterFromAssembly_WithValidParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.RegisterFromAssembly(
            typeof(BaseServiceCollectionExtensionsTests).Assembly,
            @interface => @interface == typeof(ITestService));

        // Assert
        Assert.Same(services, result);
        Assert.Single(services);
        var descriptor = services.Single();
        Assert.Equal(typeof(ITestService), descriptor.ServiceType);
        Assert.Equal(typeof(TestService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void RegisterConditional_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).RegisterConditional(() => true, _ => { }));
    }

    [Fact]
    public void RegisterConditional_WithNullCondition_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterConditional(null!, _ => { }));
    }

    [Fact]
    public void RegisterConditional_WithNullTrueRegistrations_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.RegisterConditional(() => true, null!));
    }

    [Fact]
    public void RegisterConditional_WithConditionTrue_ExecutesTrueRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var executed = false;

        // Act
        var result = services.RegisterConditional(
            () => true,
            s => { executed = true; s.AddTransient<ITestService, TestService>(); });

        // Assert
        Assert.Same(services, result);
        Assert.True(executed);
        Assert.Single(services);
    }

    [Fact]
    public void RegisterConditional_WithConditionFalse_ExecutesFalseRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var executed = false;

        // Act
        var result = services.RegisterConditional(
            () => false,
            _ => { },
            s => { executed = true; s.AddTransient<ITestService, TestService>(); });

        // Assert
        Assert.Same(services, result);
        Assert.True(executed);
        Assert.Single(services);
    }

    // Test interfaces and classes
    public interface ITestService { }
    public class TestService : ITestService { }
    public class TestService2 { }
    public interface IPipelineBehavior { }
    public class TestPipelineBehavior : IPipelineBehavior { }

    public class TestOptions
    {
        public string? Value { get; set; }
    }
}
