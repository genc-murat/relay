using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Core;
using Relay.Core.Configuration.Options;
using Relay.Core.HandlerVersioning;
using Relay.Core.Testing;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Configuration
{
    public class RelayConfigurationExtensionsTests
    {
        [Fact()]
        public void AddRelayConfiguration_RegistersConfigurationResolver()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act - Call the Relay.Core.Configuration extension method explicitly
            var result = RelayConfigurationExtensions.AddRelayConfiguration(services);

            // Verify method chaining returns services
            Assert.Same(services, result);

            // Assert - Check service collection count
            var count = services.Count;
            Assert.True(count > 0, $"Service collection should not be empty, found {count} services");

            // Verify service is registered
            var resolverDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfigurationResolver));

            if (resolverDescriptor == null)
            {
                // Debug: Print all registered services
                var allServices = string.Join(", ", services.Select(s => s.ServiceType.Name));
                Assert.Fail($"IConfigurationResolver not registered. Registered services: {allServices}");
            }

            Assert.Equal(typeof(ConfigurationResolver), resolverDescriptor.ImplementationType);

            // Build provider and resolve
            var provider = services.BuildServiceProvider();
            var resolver = provider.GetRequiredService<IConfigurationResolver>();
            Assert.NotNull(resolver);
            Assert.IsType<ConfigurationResolver>(resolver);
        }

        [Fact]
        public void ConfigureRelay_WithAction_RegistersOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ConfigureRelay(options =>
            {
                options.EnableTelemetry = false;
                options.MaxConcurrentNotificationHandlers = 10;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<RelayOptions>>().Value;

            // Assert
            Assert.False(options.EnableTelemetry);
            Assert.Equal(10, options.MaxConcurrentNotificationHandlers);
        }

        [Fact]
        public void ConfigureRelay_WithNullAction_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.ConfigureRelay((Action<RelayOptions>)null!));
        }

        [Fact]
        public void ConfigureRelay_WithConfiguration_RegistersOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("EnableTelemetry", "false"),
                new System.Collections.Generic.KeyValuePair<string, string?>("MaxConcurrentNotificationHandlers", "5")
            });
            var configuration = configurationBuilder.Build();

            // Act
            services.ConfigureRelay(configuration);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<RelayOptions>>().Value;

            // Assert
            Assert.False(options.EnableTelemetry);
            Assert.Equal(5, options.MaxConcurrentNotificationHandlers);
        }

        [Fact]
        public void ConfigureRelay_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.ConfigureRelay((IConfiguration)null!));
        }

        [Fact]
        public void ConfigureRelay_WithSectionName_RegistersOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("Relay:EnableTelemetry", "false"),
                new System.Collections.Generic.KeyValuePair<string, string?>("Relay:MaxConcurrentNotificationHandlers", "8")
            });
            var configuration = configurationBuilder.Build();

            // Act
            services.ConfigureRelay(configuration, "Relay");
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<RelayOptions>>().Value;

            // Assert
            Assert.False(options.EnableTelemetry);
            Assert.Equal(8, options.MaxConcurrentNotificationHandlers);
        }

        [Fact]
        public void ConfigureRelay_WithEmptySectionName_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => services.ConfigureRelay(configuration, ""));
        }

        [Fact]
        public void ConfigureHandler_AddsHandlerOverride()
        {
            // Arrange
            var services = new ServiceCollection();
            var handlerKey = "TestHandler.HandleAsync";

            // Act
            services.ConfigureHandler(handlerKey, options =>
            {
                options.DefaultPriority = 10;
                options.EnableCaching = true;
            });

            var provider = services.BuildServiceProvider();
            var relayOptions = provider.GetRequiredService<IOptions<RelayOptions>>().Value;

            // Assert
            Assert.Contains(handlerKey, relayOptions.HandlerOverrides.Keys);
            Assert.Equal(10, relayOptions.HandlerOverrides[handlerKey].DefaultPriority);
            Assert.True(relayOptions.HandlerOverrides[handlerKey].EnableCaching);
        }

        [Fact]
        public void ConfigureHandler_WithEmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                services.ConfigureHandler("", options => { }));
        }

        [Fact]
        public void ConfigureHandler_WithNullAction_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                services.ConfigureHandler("key", null!));
        }

        [Fact]
        public void ConfigureNotification_AddsNotificationOverride()
        {
            // Arrange
            var services = new ServiceCollection();
            var notificationKey = "TestHandler.HandleNotification";

            // Act
            services.ConfigureNotification(notificationKey, options =>
            {
                options.DefaultPriority = 5;
                options.DefaultDispatchMode = NotificationDispatchMode.Sequential;
            });

            var provider = services.BuildServiceProvider();
            var relayOptions = provider.GetRequiredService<IOptions<RelayOptions>>().Value;

            // Assert
            Assert.Contains(notificationKey, relayOptions.NotificationOverrides.Keys);
            Assert.Equal(5, relayOptions.NotificationOverrides[notificationKey].DefaultPriority);
            Assert.Equal(NotificationDispatchMode.Sequential, relayOptions.NotificationOverrides[notificationKey].DefaultDispatchMode);
        }

        [Fact]
        public void ConfigureNotification_WithEmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                services.ConfigureNotification("", options => { }));
        }

        [Fact]
        public void ConfigureNotification_WithNullAction_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                services.ConfigureNotification("key", null!));
        }

        [Fact]
        public void ConfigurePipeline_AddsPipelineOverride()
        {
            // Arrange
            var services = new ServiceCollection();
            var pipelineKey = "TestPipeline.Execute";

            // Act
            services.ConfigurePipeline(pipelineKey, options =>
            {
                options.DefaultOrder = 100;
                options.DefaultScope = PipelineScope.Requests;
            });

            var provider = services.BuildServiceProvider();
            var relayOptions = provider.GetRequiredService<IOptions<RelayOptions>>().Value;

            // Assert
            Assert.Contains(pipelineKey, relayOptions.PipelineOverrides.Keys);
            Assert.Equal(100, relayOptions.PipelineOverrides[pipelineKey].DefaultOrder);
            Assert.Equal(PipelineScope.Requests, relayOptions.PipelineOverrides[pipelineKey].DefaultScope);
        }

        [Fact]
        public void ConfigurePipeline_WithEmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                services.ConfigurePipeline("", options => { }));
        }

        [Fact]
        public void ConfigurePipeline_WithNullAction_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                services.ConfigurePipeline("key", null!));
        }

        [Fact]
        public void ValidateRelayConfiguration_RegistersValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ValidateRelayConfiguration();
            var provider = services.BuildServiceProvider();

            // Assert
            var validator = provider.GetService<IValidateOptions<RelayOptions>>();
            Assert.NotNull(validator);
            Assert.IsType<RelayOptionsValidator>(validator);
        }

        [Fact]
        public void AddRelayCaching_RegistersCachingServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRelayCaching();
            var provider = services.BuildServiceProvider();

            // Assert - Method chaining
            Assert.Same(services, result);

            // Assert - MemoryCache is registered
            var memoryCache = provider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            Assert.NotNull(memoryCache);

            // Assert - Pipeline behavior is registered
            var pipelineBehaviors = services.Where(d => d.ServiceType.IsGenericType &&
                d.ServiceType.GetGenericTypeDefinition() == typeof(Relay.Core.Contracts.Pipeline.IPipelineBehavior<,>));
            Assert.Contains(pipelineBehaviors, d => d.ImplementationType?.Name.Contains("RelayCachingPipelineBehavior") == true);
        }



        [Fact]
        public void AddRelayAuthorization_RegistersAuthorizationServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRelayAuthorization();

            // Assert - Method chaining
            Assert.Same(services, result);

            // Assert - Pipeline behavior is registered
            var pipelineBehaviors = services.Where(d => d.ServiceType.IsGenericType &&
                d.ServiceType.GetGenericTypeDefinition() == typeof(Relay.Core.Contracts.Pipeline.IPipelineBehavior<,>));
            Assert.Contains(pipelineBehaviors, d => d.ImplementationType?.Name.Contains("AuthorizationPipelineBehavior") == true);
        }

        [Fact]
        public void AddRelayRetry_RegistersRetryServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRelayRetry();

            // Assert - Method chaining
            Assert.Same(services, result);

            // Assert - Pipeline behavior is registered
            var pipelineBehaviors = services.Where(d => d.ServiceType.IsGenericType &&
                d.ServiceType.GetGenericTypeDefinition() == typeof(Relay.Core.Contracts.Pipeline.IPipelineBehavior<,>));
            Assert.Contains(pipelineBehaviors, d => d.ImplementationType?.Name.Contains("RetryPipelineBehavior") == true);
        }

        [Fact]
        public void AddRelayContractValidation_RegistersContractValidationServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRelayContractValidation();
            var provider = services.BuildServiceProvider();

            // Assert - Method chaining
            Assert.Same(services, result);

            // Assert - Contract validator is registered
            var contractValidator = provider.GetService<Relay.Core.ContractValidation.IContractValidator>();
            Assert.NotNull(contractValidator);
            Assert.IsType<Relay.Core.ContractValidation.DefaultContractValidator>(contractValidator);

            // Assert - Pipeline behavior is registered
            var pipelineBehaviors = services.Where(d => d.ServiceType.IsGenericType &&
                d.ServiceType.GetGenericTypeDefinition() == typeof(Relay.Core.Contracts.Pipeline.IPipelineBehavior<,>));
            Assert.Contains(pipelineBehaviors, d => d.ImplementationType?.Name.Contains("ContractValidationPipelineBehavior") == true);
        }

        [Fact]
        public void AddRelayDistributedTracing_RegistersDistributedTracingServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRelayDistributedTracing();

            // Assert - Method chaining
            Assert.Same(services, result);

            // Assert - Pipeline behavior is registered
            var pipelineBehaviors = services.Where(d => d.ServiceType.IsGenericType &&
                d.ServiceType.GetGenericTypeDefinition() == typeof(Relay.Core.Contracts.Pipeline.IPipelineBehavior<,>));
            Assert.Contains(pipelineBehaviors, d => d.ImplementationType?.Name.Contains("DistributedTracingPipelineBehavior") == true);
        }



        [Fact]
        public void AddRelayEventSourcing_RegistersEventSourcingServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRelayEventSourcing();

            // Assert - Method chaining
            Assert.Same(services, result);
        }

        [Fact]
        public void AddRelayMessageQueue_RegistersMessageQueueServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRelayMessageQueue();

            // Assert - Method chaining
            Assert.Same(services, result);
        }

        [Fact]
        public void ConfigureRelay_ChainedCalls_WorkCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services
                .ConfigureRelay(options => options.EnableTelemetry = false)
                .ConfigureHandler("Handler1", options => options.DefaultPriority = 1)
                .ConfigureNotification("Notification1", options => options.DefaultPriority = 2)
                .ConfigurePipeline("Pipeline1", options => options.DefaultOrder = 3)
                .ValidateRelayConfiguration();

            var provider = services.BuildServiceProvider();
            var relayOptions = provider.GetRequiredService<IOptions<RelayOptions>>().Value;

            // Assert
            Assert.False(relayOptions.EnableTelemetry);
            Assert.Equal(1, relayOptions.HandlerOverrides["Handler1"].DefaultPriority);
            Assert.Equal(2, relayOptions.NotificationOverrides["Notification1"].DefaultPriority);
            Assert.Equal(3, relayOptions.PipelineOverrides["Pipeline1"].DefaultOrder);
        }
    }
}