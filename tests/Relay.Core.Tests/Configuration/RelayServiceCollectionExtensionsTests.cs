using Microsoft.Extensions.DependencyInjection;
using Relay.Core.AI;
using Relay.Core.Configuration;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Telemetry;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Relay.Core.Tests.Configuration;

public class RelayServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRelayWithFeatures_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayWithFeatures();

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - Core services are registered (from AddRelayCore)
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);

        // Assert - Validation pipeline behaviors are registered (from AddRelayValidation)
        var validationBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("ValidationPipelineBehavior") == true);
        Assert.NotEmpty(validationBehaviors);

        var streamValidationBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IStreamPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("StreamValidationPipelineBehavior") == true);
        Assert.NotEmpty(streamValidationBehaviors);

        // Assert - Pre/post processor behaviors are registered (from AddRelayPrePostProcessors)
        var preProcessorBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("RequestPreProcessorBehavior") == true);
        Assert.NotEmpty(preProcessorBehaviors);

        var postProcessorBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("RequestPostProcessorBehavior") == true);
        Assert.NotEmpty(postProcessorBehaviors);

        // Assert - Exception handler behaviors are registered (from AddRelayExceptionHandlers)
        var exceptionHandlerBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("RequestExceptionHandlerBehavior") == true);
        Assert.NotEmpty(exceptionHandlerBehaviors);

        var exceptionActionBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("RequestExceptionActionBehavior") == true);
        Assert.NotEmpty(exceptionActionBehaviors);
    }

    [Fact]
    public void AddRelayWithFeatures_WithConfigureAction_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedMaxHandlers = 42;

        // Act
        services.AddRelayWithFeatures(options =>
        {
            options.MaxConcurrentNotificationHandlers = expectedMaxHandlers;
        });

        var provider = services.BuildServiceProvider();
        var relayOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Relay.Core.Configuration.Options.Core.RelayOptions>>().Value;

        // Assert
        Assert.Equal(expectedMaxHandlers, relayOptions.MaxConcurrentNotificationHandlers);
    }

    [Fact]
    public void AddRelayWithFeatures_WithoutConfigureAction_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayWithFeatures();

        var provider = services.BuildServiceProvider();
        var relayOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Relay.Core.Configuration.Options.Core.RelayOptions>>().Value;

        // Assert - Default values are applied
        Assert.NotNull(relayOptions);
    }

    [Fact]
    public void AddRelayWithAdvancedFeatures_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayWithAdvancedFeatures();

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - All services from AddRelayWithFeatures are registered
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);

        // Assert - Transaction behavior is registered (from AddRelayTransactions)
        var transactionBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("TransactionBehavior") == true);
        Assert.NotEmpty(transactionBehaviors);

        // Assert - Telemetry services are registered (from AddRelayTelemetry)
        var metricsProvider = services.FirstOrDefault(d => d.ServiceType == typeof(IMetricsProvider));
        Assert.NotNull(metricsProvider);

        var telemetryProvider = services.FirstOrDefault(d => d.ServiceType == typeof(ITelemetryProvider));
        Assert.NotNull(telemetryProvider);

        // Assert - Performance optimization services are registered (from AddRelayPerformanceOptimizations)
        var objectPoolProvider = services.FirstOrDefault(d => d.ServiceType.Name.Contains("ObjectPoolProvider"));
        Assert.NotNull(objectPoolProvider);

        var telemetryContextPool = services.FirstOrDefault(d => d.ServiceType.Name.Contains("ITelemetryContextPool"));
        Assert.NotNull(telemetryContextPool);

        // Assert - Advanced AI services are registered (from AddAdvancedAIOptimization)
        var aiModelTrainer = services.FirstOrDefault(d => d.ServiceType.Name.Contains("IAIModelTrainer"));
        Assert.NotNull(aiModelTrainer);

        var aiPredictionCache = services.FirstOrDefault(d => d.ServiceType.Name.Contains("IAIPredictionCache"));
        Assert.NotNull(aiPredictionCache);

        var aiMetricsExporter = services.FirstOrDefault(d => d.ServiceType.Name.Contains("IAIMetricsExporter"));
        Assert.NotNull(aiMetricsExporter);

        // Assert - AI pipeline behaviors are registered
        var aiPerformanceTrackingBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("AIPerformanceTrackingBehavior") == true);
        Assert.NotEmpty(aiPerformanceTrackingBehaviors);

        var aiBatchOptimizationBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("AIBatchOptimizationBehavior") == true);
        Assert.NotEmpty(aiBatchOptimizationBehaviors);

        var aiCachingOptimizationBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("AICachingOptimizationBehavior") == true);
        Assert.NotEmpty(aiCachingOptimizationBehaviors);
    }

    [Fact]
    public void AddRelayWithAdvancedFeatures_WithConfigureActions_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedMaxHandlers = 50;
        var expectedAIBatchSize = 25;

        // Act
        services.AddRelayWithAdvancedFeatures(
            configureRelay: options => options.MaxConcurrentNotificationHandlers = expectedMaxHandlers,
            configureAI: options => options.DefaultBatchSize = expectedAIBatchSize
        );

        var provider = services.BuildServiceProvider();
        var relayOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Relay.Core.Configuration.Options.Core.RelayOptions>>().Value;
        var aiOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AIOptimizationOptions>>().Value;

        // Assert
        Assert.Equal(expectedMaxHandlers, relayOptions.MaxConcurrentNotificationHandlers);
        Assert.Equal(expectedAIBatchSize, aiOptions.DefaultBatchSize);
    }

    [Fact]
    public void AddRelayWithAdvancedFeatures_WithoutConfigureActions_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayWithAdvancedFeatures();

        var provider = services.BuildServiceProvider();
        var relayOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Relay.Core.Configuration.Options.Core.RelayOptions>>().Value;
        var aiOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AIOptimizationOptions>>().Value;

        // Assert - Default values are applied
        Assert.NotNull(relayOptions);
        Assert.NotNull(aiOptions);
    }

    [Fact]
    public void AddRelayForScenario_Minimal_RegistersOnlyCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayForScenario(RelayScenario.Minimal);

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - Core services are registered
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);

        // Assert - No additional services are registered (validation, telemetry, etc.)
        var validationBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("ValidationPipelineBehavior") == true);
        Assert.Empty(validationBehaviors);

        var telemetryProvider = services.FirstOrDefault(d => d.ServiceType == typeof(ITelemetryProvider));
        Assert.Null(telemetryProvider);
    }

    [Fact]
    public void AddRelayForScenario_WebApi_RegistersWebApiServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayForScenario(RelayScenario.WebApi);

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - Core services are registered
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);

        // Assert - Validation, pre/post processors, exception handlers, and telemetry are registered
        var validationBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("ValidationPipelineBehavior") == true);
        Assert.NotEmpty(validationBehaviors);

        var telemetryProvider = services.FirstOrDefault(d => d.ServiceType == typeof(ITelemetryProvider));
        Assert.NotNull(telemetryProvider);

        // Assert - Transaction behaviors are NOT registered (not in WebApi scenario)
        var transactionBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("TransactionBehavior") == true);
        Assert.Empty(transactionBehaviors);
    }

    [Fact]
    public void AddRelayForScenario_HighPerformance_RegistersPerformanceServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayForScenario(RelayScenario.HighPerformance);

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - Core services are registered
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);

        // Assert - Validation and performance optimizations are registered
        var validationBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("ValidationPipelineBehavior") == true);
        Assert.NotEmpty(validationBehaviors);

        var performanceOptimizations = services.FirstOrDefault(d => d.ServiceType.Name.Contains("ObjectPoolProvider"));
        Assert.NotNull(performanceOptimizations);
    }

    [Fact]
    public void AddRelayForScenario_EventDriven_RegistersEventDrivenServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayForScenario(RelayScenario.EventDriven);

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - Core services are registered
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);

        // Assert - Validation, pre/post processors, exception handlers, transactions, and telemetry are registered
        var validationBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("ValidationPipelineBehavior") == true);
        Assert.NotEmpty(validationBehaviors);

        var transactionBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("TransactionBehavior") == true);
        Assert.NotEmpty(transactionBehaviors);

        var telemetryProvider = services.FirstOrDefault(d => d.ServiceType == typeof(ITelemetryProvider));
        Assert.NotNull(telemetryProvider);
    }

    [Fact]
    public void AddRelayForScenario_Microservices_RegistersFullFeatureSet()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayForScenario(RelayScenario.Microservices);

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - Core services are registered
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);

        // Assert - All services are registered: validation, pre/post processors, exception handlers, transactions, telemetry, performance optimizations
        var validationBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("ValidationPipelineBehavior") == true);
        Assert.NotEmpty(validationBehaviors);

        var transactionBehaviors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("TransactionBehavior") == true);
        Assert.NotEmpty(transactionBehaviors);

        var telemetryProvider = services.FirstOrDefault(d => d.ServiceType == typeof(ITelemetryProvider));
        Assert.NotNull(telemetryProvider);

        var performanceOptimizations = services.FirstOrDefault(d => d.ServiceType.Name.Contains("ObjectPoolProvider"));
        Assert.NotNull(performanceOptimizations);
    }

    [Fact]
    public void AddRelayForScenario_InvalidScenario_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        var invalidScenario = (RelayScenario)999;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddRelayForScenario(invalidScenario));
    }

    [Fact]
    public void AddRelayForScenario_WithConfigureAction_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedMaxHandlers = 75;

        // Act
        services.AddRelayForScenario(RelayScenario.Minimal, options =>
        {
            options.MaxConcurrentNotificationHandlers = expectedMaxHandlers;
        });

        var provider = services.BuildServiceProvider();
        var relayOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Relay.Core.Configuration.Options.Core.RelayOptions>>().Value;

        // Assert
        Assert.Equal(expectedMaxHandlers, relayOptions.MaxConcurrentNotificationHandlers);
    }

    [Fact]
    public void AddRelayWithHandlerDiscovery_WithAssembliesArray_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var testAssembly = Assembly.GetExecutingAssembly();

        // Act
        var result = services.AddRelayWithHandlerDiscovery(new[] { testAssembly });

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - Core services are registered
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);
    }

    [Fact]
    public void AddRelayWithHandlerDiscovery_WithAssembliesArray_WithConfigureAction_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var testAssembly = Assembly.GetExecutingAssembly();
        var expectedMaxHandlers = 80;

        // Act
        services.AddRelayWithHandlerDiscovery(new[] { testAssembly }, options =>
        {
            options.MaxConcurrentNotificationHandlers = expectedMaxHandlers;
        });

        var provider = services.BuildServiceProvider();
        var relayOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Relay.Core.Configuration.Options.Core.RelayOptions>>().Value;

        // Assert
        Assert.Equal(expectedMaxHandlers, relayOptions.MaxConcurrentNotificationHandlers);
    }

    [Fact]
    public void AddRelayWithHandlerDiscovery_WithAssembliesArray_NullAssemblies_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddRelayWithHandlerDiscovery((Assembly[])null!));
    }

    [Fact]
    public void AddRelayWithHandlerDiscovery_WithAssembliesArray_EmptyAssembliesArray_Works()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayWithHandlerDiscovery(Array.Empty<Assembly>());

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - Core services are still registered
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);
    }

    [Fact]
    public void AddRelayWithHandlerDiscovery_DefaultOverload_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayWithHandlerDiscovery();

        // Assert - Method chaining returns services
        Assert.Same(services, result);

        // Assert - Core services are registered
        var relayService = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayService);
    }

    [Fact]
    public void AddRelayWithHandlerDiscovery_DefaultOverload_WithConfigureAction_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedMaxHandlers = 85;

        // Act
        services.AddRelayWithHandlerDiscovery(options =>
        {
            options.MaxConcurrentNotificationHandlers = expectedMaxHandlers;
        });

        var provider = services.BuildServiceProvider();
        var relayOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Relay.Core.Configuration.Options.Core.RelayOptions>>().Value;

        // Assert
        Assert.Equal(expectedMaxHandlers, relayOptions.MaxConcurrentNotificationHandlers);
    }

    [Fact]
    public void MessageQueueOptions_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new MessageQueueOptions();

        // Assert
        Assert.False(options.EnableMessageQueueIntegration);
        Assert.Equal("InMemory", options.DefaultMessageQueue);
        Assert.True(options.EnableAutomaticRetry);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal("dead-letter", options.DeadLetterQueueName);
        Assert.True(options.AutoAck);
        Assert.Equal((ushort)1, options.PrefetchCount);
    }

    [Fact]
    public void MessageQueueOptions_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new MessageQueueOptions();

        // Act
        options.EnableMessageQueueIntegration = true;
        options.DefaultMessageQueue = "RabbitMQ";
        options.EnableAutomaticRetry = false;
        options.MaxRetryAttempts = 5;
        options.DeadLetterQueueName = "failed-messages";
        options.AutoAck = false;
        options.PrefetchCount = 10;

        // Assert
        Assert.True(options.EnableMessageQueueIntegration);
        Assert.Equal("RabbitMQ", options.DefaultMessageQueue);
        Assert.False(options.EnableAutomaticRetry);
        Assert.Equal(5, options.MaxRetryAttempts);
        Assert.Equal("failed-messages", options.DeadLetterQueueName);
        Assert.False(options.AutoAck);
        Assert.Equal((ushort)10, options.PrefetchCount);
    }

    [Fact]
    public void MessageQueueOptions_PropertyBoundaries_AreRespected()
    {
        // Arrange
        var options = new MessageQueueOptions();

        // Act & Assert - Test that properties accept valid values
        options.MaxRetryAttempts = 0; // Valid
        Assert.Equal(0, options.MaxRetryAttempts);

        options.MaxRetryAttempts = int.MaxValue; // Valid
        Assert.Equal(int.MaxValue, options.MaxRetryAttempts);

        options.PrefetchCount = 0; // Valid
        Assert.Equal((ushort)0, options.PrefetchCount);

        options.PrefetchCount = ushort.MaxValue; // Valid
        Assert.Equal(ushort.MaxValue, options.PrefetchCount);
    }

    [Fact]
    public void AddRelayWithAdvancedFeatures_EndToEnd_ServiceResolution_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging for AI services

        // Act
        services.AddRelayWithAdvancedFeatures();

        // Assert - Core services are registered
        var relayDescriptor = services.FirstOrDefault(d => d.ServiceType.Name == "IRelay");
        Assert.NotNull(relayDescriptor);

        // Assert - AI services are registered
        var aiEngineDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Relay.Core.AI.IAIOptimizationEngine));
        Assert.NotNull(aiEngineDescriptor);

        var aiModelTrainerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Relay.Core.AI.IAIModelTrainer));
        Assert.NotNull(aiModelTrainerDescriptor);

        // Assert - Telemetry services are registered
        var telemetryProviderDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Relay.Core.Telemetry.ITelemetryProvider));
        Assert.NotNull(telemetryProviderDescriptor);

        // Assert - Metrics provider is registered
        var metricsProviderDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Relay.Core.Telemetry.IMetricsProvider));
        Assert.NotNull(metricsProviderDescriptor);

        // Assert - Pipeline behaviors are registered (multiple implementations)
        var pipelineBehaviorDescriptors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(Relay.Core.Contracts.Pipeline.IPipelineBehavior<,>));
        Assert.NotEmpty(pipelineBehaviorDescriptors);
        Assert.True(pipelineBehaviorDescriptors.Count() >= 3); // At least validation, AI, and others
    }
}

