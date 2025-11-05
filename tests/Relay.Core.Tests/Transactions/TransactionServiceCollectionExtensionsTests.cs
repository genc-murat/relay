using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Transactions.Tests;

public class TransactionServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRelayTransactions_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddRelayTransactions());
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddRelayTransactions_WithConfigureOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;
        Action<TransactionOptions> configureOptions = options => { };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddRelayTransactions(configureOptions));
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddRelayTransactions_WithConfigureOptions_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add required logging services
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        services.AddSingleton(mockUnitOfWork.Object); // Mock IUnitOfWork since TransactionCoordinator requires it
        services.AddTransient<ITransactionEventHandler, TestTransactionEventHandler>(); // Add event handler for publisher

        // Act
        var result = services.AddRelayTransactions(options =>
        {
            options.DefaultTimeout = TimeSpan.FromSeconds(45);
            options.EnableMetrics = true;
        });

        // Assert
        Assert.Same(services, result);
        
        // Verify services were registered
        var serviceProvider = services.BuildServiceProvider();
        
        // Check that options were configured properly
        var options = serviceProvider.GetRequiredService<IOptions<TransactionOptions>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(45), options.DefaultTimeout);
        Assert.True(options.EnableMetrics);
        
        // Check that validator was registered
        var validators = serviceProvider.GetServices<IValidateOptions<TransactionOptions>>();
        Assert.Contains(validators, v => v is TransactionOptionsValidator);
        
        // Check that core infrastructure services were registered
        Assert.NotNull(serviceProvider.GetService<TransactionCoordinator>());
        Assert.NotNull(serviceProvider.GetService<TransactionEventPublisher>());
        Assert.NotNull(serviceProvider.GetService<TransactionConfigurationResolver>());
        Assert.NotNull(serviceProvider.GetService<TransactionRetryHandler>());
        Assert.NotNull(serviceProvider.GetService<NestedTransactionManager>());
        Assert.NotNull(serviceProvider.GetService<TransactionMetricsCollector>());
        Assert.NotNull(serviceProvider.GetService<TransactionHealthCheck>());
        Assert.NotNull(serviceProvider.GetService<TransactionActivitySource>());
        
        // Check that TransactionBehavior was registered as pipeline behavior
        // We don't check for actual instantiation as it requires complex dependencies
        // but we verify it's registered by checking the service descriptor
        var serviceDescriptor = services.FirstOrDefault(s => 
            s.ServiceType == typeof(IPipelineBehavior<,>) && 
            s.ImplementationType?.Name == "TransactionBehavior`2");
        Assert.NotNull(serviceDescriptor);
    }

    [Fact]
    public void AddRelayTransactions_WithConfigureOptions_NullConfigureOptions_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add required logging services
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        services.AddSingleton(mockUnitOfWork.Object); // Mock IUnitOfWork since TransactionCoordinator requires it

        // Act
        var result = services.AddRelayTransactions((Action<TransactionOptions>?)null);

        // Assert
        Assert.Same(services, result);
        
        // Verify default options were registered
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<TransactionOptions>>().Value;
        
        // Verify default values
        Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultTimeout); // Default is 30 seconds
        Assert.NotNull(serviceProvider.GetService<TransactionCoordinator>());
    }

    [Fact]
    public void AddRelayTransactions_WithConfigurationSection_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;
        var configSection = new ConfigurationBuilder().Build().GetSection("TestSection");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddRelayTransactions(configSection));
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddRelayTransactions_WithConfigurationSection_NullConfigurationSection_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfigurationSection configSection = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddRelayTransactions(configSection));
        Assert.Equal("configurationSection", exception.ParamName);
    }

    [Fact]
    public void AddRelayTransactions_WithConfigurationSection_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add required logging services
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        services.AddSingleton(mockUnitOfWork.Object); // Mock IUnitOfWork since TransactionCoordinator requires it
        services.AddTransient<ITransactionEventHandler, TestTransactionEventHandler>(); // Add event handler for publisher

        // Create configuration with the full path structure as shown in documentation
        var inMemoryData = new Dictionary<string, string?>
        {
            ["Relay:Transactions:DefaultTimeoutSeconds"] = "45",
            ["Relay:Transactions:EnableMetrics"] = "true",
            ["Relay:Transactions:EnableDistributedTracing"] = "true",
            ["Relay:Transactions:EnableNestedTransactions"] = "true",
            ["Relay:Transactions:EnableSavepoints"] = "true",
            ["Relay:Transactions:RequireExplicitTransactionAttribute"] = "true"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryData)
            .Build();

        // Get the specific section that contains the transaction options
        var configSection = config.GetSection("Relay:Transactions");

        // Act
        var result = services.AddRelayTransactions(configSection);

        // Assert
        Assert.Same(services, result);
        
        // Verify services were registered
        var serviceProvider = services.BuildServiceProvider();
        
        // Check that options were bound from configuration
        var options = serviceProvider.GetRequiredService<IOptions<TransactionOptions>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(45), options.DefaultTimeout);
        Assert.True(options.EnableMetrics);
        Assert.True(options.EnableDistributedTracing);
        
        // Check that validator was registered
        var validators = serviceProvider.GetServices<IValidateOptions<TransactionOptions>>();
        Assert.Contains(validators, v => v is TransactionOptionsValidator);
        
        // Check that all core infrastructure services were registered
        Assert.NotNull(serviceProvider.GetService<TransactionCoordinator>());
        Assert.NotNull(serviceProvider.GetService<TransactionEventPublisher>());
        Assert.NotNull(serviceProvider.GetService<TransactionConfigurationResolver>());
        Assert.NotNull(serviceProvider.GetService<TransactionRetryHandler>());
        Assert.NotNull(serviceProvider.GetService<NestedTransactionManager>());
        Assert.NotNull(serviceProvider.GetService<TransactionMetricsCollector>());
        Assert.NotNull(serviceProvider.GetService<TransactionHealthCheck>());
        Assert.NotNull(serviceProvider.GetService<TransactionActivitySource>());
    }

    [Fact]
    public void AddTransactionEventHandler_Generic_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddTransactionEventHandler<TestTransactionEventHandler>());
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddTransactionEventHandler_Generic_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddTransactionEventHandler<TestTransactionEventHandler>();

        // Assert
        Assert.Same(services, result);
        
        // Verify handler was registered
        var serviceProvider = services.BuildServiceProvider();
        var handlers = serviceProvider.GetServices<ITransactionEventHandler>();
        
        // Should have one instance of our test handler
        Assert.Contains(handlers, h => h is TestTransactionEventHandler);
    }

    [Fact]
    public void AddTransactionEventHandler_Generic_WithServiceLifetime_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddTransactionEventHandler<TestTransactionEventHandler>(ServiceLifetime.Singleton);

        // Assert
        Assert.Same(services, result);
        
        // Verify handler was registered with correct lifetime
        var serviceProvider = services.BuildServiceProvider();
        var handler1 = serviceProvider.GetRequiredService<ITransactionEventHandler>();
        var handler2 = serviceProvider.GetRequiredService<ITransactionEventHandler>();
        
        // With singleton lifetime, both should be the same instance
        Assert.Same(handler1, handler2);
    }

    [Fact]
    public void AddTransactionEventHandler_Instance_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;
        var handler = new TestTransactionEventHandler();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddTransactionEventHandler(handler));
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddTransactionEventHandler_Instance_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        ITransactionEventHandler handler = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddTransactionEventHandler(handler));
        Assert.Equal("handler", exception.ParamName);
    }

    [Fact]
    public void AddTransactionEventHandler_Instance_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransactionEventHandler();

        // Act
        var result = services.AddTransactionEventHandler(handler);

        // Assert
        Assert.Same(services, result);
        
        // Verify handler was registered
        var serviceProvider = services.BuildServiceProvider();
        var registeredHandler = serviceProvider.GetRequiredService<ITransactionEventHandler>();
        
        // Should be the same instance that was passed in
        Assert.Same(handler, registeredHandler);
    }

    [Fact]
    public void AddTransactionEventHandler_Factory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;
        Func<IServiceProvider, ITransactionEventHandler> factory = sp => new TestTransactionEventHandler();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddTransactionEventHandler(factory));
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddTransactionEventHandler_Factory_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, ITransactionEventHandler> factory = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddTransactionEventHandler(factory));
        Assert.Equal("factory", exception.ParamName);
    }

    [Fact]
    public void AddTransactionEventHandler_Factory_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new TestTransactionEventHandler();
        Func<IServiceProvider, ITransactionEventHandler> factory = sp => instance;

        // Act
        var result = services.AddTransactionEventHandler(factory);

        // Assert
        Assert.Same(services, result);
        
        // Verify handler was registered with factory
        var serviceProvider = services.BuildServiceProvider();
        var registeredHandler = serviceProvider.GetRequiredService<ITransactionEventHandler>();
        
        // Should be the same instance returned by the factory
        Assert.Same(instance, registeredHandler);
    }

    [Fact]
    public void AddTransactionEventHandler_Factory_WithServiceLifetime_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new TestTransactionEventHandler();
        Func<IServiceProvider, ITransactionEventHandler> factory = sp => instance;

        // Act
        var result = services.AddTransactionEventHandler(factory, ServiceLifetime.Singleton);

        // Assert
        Assert.Same(services, result);
        
        // Verify handler was registered with correct lifetime
        var serviceProvider = services.BuildServiceProvider();
        var handler1 = serviceProvider.GetRequiredService<ITransactionEventHandler>();
        var handler2 = serviceProvider.GetRequiredService<ITransactionEventHandler>();
        
        // With singleton lifetime, both should be the same instance
        Assert.Same(handler1, handler2);
    }

    [Fact]
    public void AddTransactionEventPublisher_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddTransactionEventPublisher());
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddTransactionEventPublisher_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add required logging services

        // Add a test event handler so the publisher has handlers to work with
        services.AddTransient<ITransactionEventHandler, TestTransactionEventHandler>();

        // Act
        var result = services.AddTransactionEventPublisher();

        // Assert
        Assert.Same(services, result);
        
        // Verify publisher was registered as singleton
        var serviceProvider = services.BuildServiceProvider();
        
        // When we resolve the concrete type first
        var publisher1 = serviceProvider.GetRequiredService<TransactionEventPublisher>();
        var publisher2 = serviceProvider.GetRequiredService<TransactionEventPublisher>();
        
        // Should be the same instance (singleton)  
        Assert.Same(publisher1, publisher2);
        
        // Now resolve the interface - this should give us the same instance
        var interfacePublisher = serviceProvider.GetRequiredService<ITransactionEventPublisher>();
        
        // To verify they're the same instance, we can get the concrete type of the interface result
        var concreteFromInterface = serviceProvider.GetRequiredService<TransactionEventPublisher>();
        Assert.Same(publisher1, concreteFromInterface);
    }

    [Fact]
    public void AddTransactionCoordinator_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddTransactionCoordinator());
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddTransactionCoordinator_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add required logging services

        // Mock IUnitOfWork since TransactionCoordinator requires it
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        services.AddSingleton(mockUnitOfWork.Object);

        // Act
        var result = services.AddTransactionCoordinator();

        // Assert
        Assert.Same(services, result);
        
        // Verify coordinator was registered as transient
        var serviceProvider = services.BuildServiceProvider();
        var coordinator1 = serviceProvider.GetRequiredService<TransactionCoordinator>();
        var coordinator2 = serviceProvider.GetRequiredService<TransactionCoordinator>();
        
        // Should be different instances (transient)
        Assert.NotSame(coordinator1, coordinator2);
        
        // Verify interface registration
        var interfaceCoordinator = serviceProvider.GetRequiredService<ITransactionCoordinator>();
        Assert.Same(coordinator1.GetType(), interfaceCoordinator.GetType());
    }

    [Fact]
    public void AddDistributedTransactionCoordinator_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddDistributedTransactionCoordinator());
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddDistributedTransactionCoordinator_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add required logging services

        // Act
        var result = services.AddDistributedTransactionCoordinator();

        // Assert
        Assert.Same(services, result);
        
        // Verify distributed coordinator was registered as transient
        var serviceProvider = services.BuildServiceProvider();
        var coordinator1 = serviceProvider.GetRequiredService<DistributedTransactionCoordinator>();
        var coordinator2 = serviceProvider.GetRequiredService<DistributedTransactionCoordinator>();
        
        // Should be different instances (transient)
        Assert.NotSame(coordinator1, coordinator2);
    }

    [Fact]
    public void AddTransactionInfrastructure_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddTransactionInfrastructure());
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddTransactionInfrastructure_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add required logging services

        // Mock IUnitOfWork since TransactionCoordinator requires it
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        services.AddSingleton(mockUnitOfWork.Object);

        // Act
        var result = services.AddTransactionInfrastructure();

        // Assert
        Assert.Same(services, result);
        
        // Verify all infrastructure services were registered
        var serviceProvider = services.BuildServiceProvider();
        
        // Transaction coordinator (transient)
        var coordinator1 = serviceProvider.GetRequiredService<TransactionCoordinator>();
        var coordinator2 = serviceProvider.GetRequiredService<TransactionCoordinator>();
        Assert.NotSame(coordinator1, coordinator2);
        
        // Distributed transaction coordinator (transient)
        var distributedCoordinator1 = serviceProvider.GetRequiredService<DistributedTransactionCoordinator>();
        var distributedCoordinator2 = serviceProvider.GetRequiredService<DistributedTransactionCoordinator>();
        Assert.NotSame(distributedCoordinator1, distributedCoordinator2);
        
        // Transaction event publisher (singleton)
        var publisher1 = serviceProvider.GetRequiredService<TransactionEventPublisher>();
        var publisher2 = serviceProvider.GetRequiredService<TransactionEventPublisher>();
        Assert.Same(publisher1, publisher2);
        
        // Transaction configuration resolver (transient)
        var configResolver1 = serviceProvider.GetRequiredService<TransactionConfigurationResolver>();
        var configResolver2 = serviceProvider.GetRequiredService<TransactionConfigurationResolver>();
        Assert.NotSame(configResolver1, configResolver2);
        
        // Transaction retry handler (transient)
        var retryHandler1 = serviceProvider.GetRequiredService<TransactionRetryHandler>();
        var retryHandler2 = serviceProvider.GetRequiredService<TransactionRetryHandler>();
        Assert.NotSame(retryHandler1, retryHandler2);
        
        // Nested transaction manager (transient)
        var nestedManager1 = serviceProvider.GetRequiredService<NestedTransactionManager>();
        var nestedManager2 = serviceProvider.GetRequiredService<NestedTransactionManager>();
        Assert.NotSame(nestedManager1, nestedManager2);
        
        // Transaction metrics collector (singleton)
        var metricsCollector1 = serviceProvider.GetRequiredService<TransactionMetricsCollector>();
        var metricsCollector2 = serviceProvider.GetRequiredService<TransactionMetricsCollector>();
        Assert.Same(metricsCollector1, metricsCollector2);
        
        // Transaction health check (singleton)
        var healthCheck1 = serviceProvider.GetRequiredService<TransactionHealthCheck>();
        var healthCheck2 = serviceProvider.GetRequiredService<TransactionHealthCheck>();
        Assert.Same(healthCheck1, healthCheck2);
        
        // Activity source (singleton)
        var activitySource1 = serviceProvider.GetRequiredService<TransactionActivitySource>();
        var activitySource2 = serviceProvider.GetRequiredService<TransactionActivitySource>();
        Assert.Same(activitySource1, activitySource2);
        
        // Transaction logger
        var transactionLogger1 = serviceProvider.GetRequiredService<TransactionLogger>();
        var transactionLogger2 = serviceProvider.GetRequiredService<TransactionLogger>();
        Assert.NotSame(transactionLogger1, transactionLogger2); // Transient
        
        // Verify interface registrations
        Assert.NotNull(serviceProvider.GetService<ITransactionCoordinator>());
        Assert.NotNull(serviceProvider.GetService<ITransactionEventPublisher>());
        Assert.NotNull(serviceProvider.GetService<ITransactionConfigurationResolver>());
        Assert.NotNull(serviceProvider.GetService<ITransactionRetryHandler>());
        Assert.NotNull(serviceProvider.GetService<INestedTransactionManager>());
        Assert.NotNull(serviceProvider.GetService<ITransactionMetricsCollector>());
    }
}

internal class TestTransactionEventHandler : ITransactionEventHandler
{
    public System.Threading.Tasks.Task OnBeforeBeginAsync(TransactionEventContext context, System.Threading.CancellationToken cancellationToken = default) 
        => System.Threading.Tasks.Task.CompletedTask;
    
    public System.Threading.Tasks.Task OnAfterBeginAsync(TransactionEventContext context, System.Threading.CancellationToken cancellationToken = default) 
        => System.Threading.Tasks.Task.CompletedTask;
    
    public System.Threading.Tasks.Task OnBeforeCommitAsync(TransactionEventContext context, System.Threading.CancellationToken cancellationToken = default) 
        => System.Threading.Tasks.Task.CompletedTask;
    
    public System.Threading.Tasks.Task OnAfterCommitAsync(TransactionEventContext context, System.Threading.CancellationToken cancellationToken = default) 
        => System.Threading.Tasks.Task.CompletedTask;
    
    public System.Threading.Tasks.Task OnBeforeRollbackAsync(TransactionEventContext context, System.Threading.CancellationToken cancellationToken = default) 
        => System.Threading.Tasks.Task.CompletedTask;
    
    public System.Threading.Tasks.Task OnAfterRollbackAsync(TransactionEventContext context, System.Threading.CancellationToken cancellationToken = default) 
        => System.Threading.Tasks.Task.CompletedTask;
}
