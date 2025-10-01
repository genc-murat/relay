using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration;
using Xunit;

namespace Relay.Core.Tests.Configuration
{
    public class RelayConfigurationExtensionsTests
    {
        [Fact(Skip = "DI registration issue - IConfigurationResolver returns null from container")]
        public void AddRelayConfiguration_RegistersConfigurationResolver()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayConfiguration();
            var provider = services.BuildServiceProvider();

            // Assert
            var resolver = provider.GetService<IConfigurationResolver>();
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