using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Core;
using Relay.Core.Configuration.Options;
using Relay.Core.Configuration.Options.Core;
using Xunit;

namespace Relay.Core.Tests.Configuration
{
    public class ConfigurationIntegrationTests
    {
        [Fact]
        public void ConfigurationSystem_Integration_WorksCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ConfigureRelay(options =>
            {
                options.EnableTelemetry = false;
                options.DefaultHandlerOptions.DefaultPriority = 10;
                options.DefaultNotificationOptions.DefaultDispatchMode = NotificationDispatchMode.Sequential;
            });

            var provider = services.BuildServiceProvider();
            var resolver = provider.GetRequiredService<IConfigurationResolver>();
            var options = provider.GetRequiredService<IOptions<RelayOptions>>().Value;

            // Assert - Options are configured correctly
            Assert.False(options.EnableTelemetry);
            Assert.Equal(10, options.DefaultHandlerOptions.DefaultPriority);
            Assert.Equal(NotificationDispatchMode.Sequential, options.DefaultNotificationOptions.DefaultDispatchMode);

            // Assert - Resolver works correctly
            var handlerConfig = resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", null);
            Assert.Equal(10, handlerConfig.Priority);

            var notificationConfig = resolver.ResolveNotificationConfiguration(typeof(TestHandler), "HandleNotification", null);
            Assert.Equal(NotificationDispatchMode.Sequential, notificationConfig.DispatchMode);
        }

        [Fact]
        public void ConfigurationSystem_AttributeOverrides_WorkCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.ConfigureRelay(options =>
            {
                options.DefaultHandlerOptions.DefaultPriority = 5;
            });

            var provider = services.BuildServiceProvider();
            var resolver = provider.GetRequiredService<IConfigurationResolver>();

            var attribute = new HandleAttribute { Priority = 20 };

            // Act
            var config = resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", attribute);

            // Assert
            Assert.Equal(20, config.Priority); // Attribute overrides global default
        }

        [Fact]
        public void ConfigurationSystem_SpecificOverrides_WorkCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var handlerKey = $"{typeof(TestHandler).FullName}.HandleAsync";

            services.ConfigureRelay(options =>
            {
                options.DefaultHandlerOptions.DefaultPriority = 5;
            });

            services.ConfigureHandler(handlerKey, options =>
            {
                options.DefaultPriority = 15;
                options.EnableCaching = true;
            });

            var provider = services.BuildServiceProvider();
            var resolver = provider.GetRequiredService<IConfigurationResolver>();

            // Act
            var config = resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", null);

            // Assert
            Assert.Equal(15, config.Priority); // Specific override wins over global default
            Assert.True(config.EnableCaching);
        }

        [Fact]
        public void ConfigurationSystem_PrecedenceOrder_WorksCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var handlerKey = $"{typeof(TestHandler).FullName}.HandleAsync";

            // Global default: 5
            services.ConfigureRelay(options =>
            {
                options.DefaultHandlerOptions.DefaultPriority = 5;
            });

            // Specific override: 15
            services.ConfigureHandler(handlerKey, options =>
            {
                options.DefaultPriority = 15;
            });

            var provider = services.BuildServiceProvider();
            var resolver = provider.GetRequiredService<IConfigurationResolver>();

            // Attribute: 25 (highest priority)
            var attribute = new HandleAttribute { Priority = 25 };

            // Act
            var config = resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", attribute);

            // Assert
            Assert.Equal(25, config.Priority); // Attribute has highest priority
        }

        private class TestHandler { }
    }
}

