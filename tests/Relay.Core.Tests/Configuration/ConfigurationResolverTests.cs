using System;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration;
using Xunit;

namespace Relay.Core.Tests.Configuration
{
    public class ConfigurationResolverTests
    {
        private readonly ConfigurationResolver _resolver;
        private readonly RelayOptions _options;

        public ConfigurationResolverTests()
        {
            _options = new RelayOptions();
            var optionsWrapper = Options.Create(_options);
            _resolver = new ConfigurationResolver(optionsWrapper);
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ConfigurationResolver(null!));
        }

        [Fact]
        public void ResolveHandlerConfiguration_WithNullHandlerType_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _resolver.ResolveHandlerConfiguration(null!, "method", null));
        }

        [Fact]
        public void ResolveHandlerConfiguration_WithEmptyMethodName_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _resolver.ResolveHandlerConfiguration(typeof(string), "", null));
        }

        [Fact]
        public void ResolveHandlerConfiguration_WithNoAttribute_UsesGlobalDefaults()
        {
            // Arrange
            _options.DefaultHandlerOptions.DefaultPriority = 5;
            _options.DefaultHandlerOptions.EnableCaching = true;
            _options.DefaultHandlerOptions.DefaultTimeout = TimeSpan.FromSeconds(30);

            // Act
            var result = _resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", null);

            // Assert
            Assert.Null(result.Name);
            Assert.Equal(5, result.Priority);
            Assert.True(result.EnableCaching);
            Assert.Equal(TimeSpan.FromSeconds(30), result.Timeout);
        }

        [Fact]
        public void ResolveHandlerConfiguration_WithAttribute_UsesAttributeValues()
        {
            // Arrange
            var attribute = new HandleAttribute { Name = "TestHandler", Priority = 10 };

            // Act
            var result = _resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", attribute);

            // Assert
            Assert.Equal("TestHandler", result.Name);
            Assert.Equal(10, result.Priority);
        }

        [Fact]
        public void ResolveHandlerConfiguration_WithSpecificOverrides_UsesOverrides()
        {
            // Arrange
            var key = $"{typeof(TestHandler).FullName}.HandleAsync";
            _options.HandlerOverrides[key] = new HandlerOptions
            {
                DefaultPriority = 15,
                EnableCaching = true,
                DefaultTimeout = TimeSpan.FromMinutes(1)
            };

            // Act
            var result = _resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", null);

            // Assert
            Assert.Equal(15, result.Priority);
            Assert.True(result.EnableCaching);
            Assert.Equal(TimeSpan.FromMinutes(1), result.Timeout);
        }

        [Fact]
        public void ResolveHandlerConfiguration_AttributeOverridesSpecificOverrides()
        {
            // Arrange
            var key = $"{typeof(TestHandler).FullName}.HandleAsync";
            _options.HandlerOverrides[key] = new HandlerOptions { DefaultPriority = 15 };
            var attribute = new HandleAttribute { Priority = 20 };

            // Act
            var result = _resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", attribute);

            // Assert
            Assert.Equal(20, result.Priority); // Attribute wins
        }

        [Fact]
        public void ResolveNotificationConfiguration_WithNoAttribute_UsesGlobalDefaults()
        {
            // Arrange
            _options.DefaultNotificationOptions.DefaultDispatchMode = NotificationDispatchMode.Sequential;
            _options.DefaultNotificationOptions.DefaultPriority = 3;
            _options.DefaultNotificationOptions.ContinueOnError = false;

            // Act
            var result = _resolver.ResolveNotificationConfiguration(typeof(TestHandler), "HandleNotification", null);

            // Assert
            Assert.Equal(NotificationDispatchMode.Sequential, result.DispatchMode);
            Assert.Equal(3, result.Priority);
            Assert.False(result.ContinueOnError);
        }

        [Fact]
        public void ResolveNotificationConfiguration_WithAttribute_UsesAttributeValues()
        {
            // Arrange
            var attribute = new NotificationAttribute
            {
                DispatchMode = NotificationDispatchMode.Parallel,
                Priority = 7
            };

            // Act
            var result = _resolver.ResolveNotificationConfiguration(typeof(TestHandler), "HandleNotification", attribute);

            // Assert
            Assert.Equal(NotificationDispatchMode.Parallel, result.DispatchMode);
            Assert.Equal(7, result.Priority);
        }

        [Fact]
        public void ResolvePipelineConfiguration_WithNoAttribute_UsesGlobalDefaults()
        {
            // Arrange
            _options.DefaultPipelineOptions.DefaultOrder = 100;
            _options.DefaultPipelineOptions.DefaultScope = PipelineScope.Requests;
            _options.DefaultPipelineOptions.EnableCaching = true;

            // Act
            var result = _resolver.ResolvePipelineConfiguration(typeof(TestPipeline), "Execute", null);

            // Assert
            Assert.Equal(100, result.Order);
            Assert.Equal(PipelineScope.Requests, result.Scope);
            Assert.True(result.EnableCaching);
        }

        [Fact]
        public void ResolvePipelineConfiguration_WithAttribute_UsesAttributeValues()
        {
            // Arrange
            var attribute = new PipelineAttribute
            {
                Order = 50,
                Scope = PipelineScope.Streams
            };

            // Act
            var result = _resolver.ResolvePipelineConfiguration(typeof(TestPipeline), "Execute", attribute);

            // Assert
            Assert.Equal(50, result.Order);
            Assert.Equal(PipelineScope.Streams, result.Scope);
        }

        [Fact]
        public void ResolveEndpointConfiguration_WithNoAttribute_UsesGlobalDefaults()
        {
            // Arrange
            _options.DefaultEndpointOptions.DefaultHttpMethod = "GET";
            _options.DefaultEndpointOptions.DefaultRoutePrefix = "api/v1";
            _options.DefaultEndpointOptions.EnableAutoRouteGeneration = true;

            // Act
            var result = _resolver.ResolveEndpointConfiguration(typeof(TestHandler), "HandleAsync", null);

            // Assert
            Assert.Equal("GET", result.HttpMethod);
            Assert.Equal("api/v1/test/handleasync", result.Route); // Auto-generated
            Assert.True(result.EnableAutoRouteGeneration);
        }

        [Fact]
        public void ResolveEndpointConfiguration_WithAttribute_UsesAttributeValues()
        {
            // Arrange
            var attribute = new ExposeAsEndpointAttribute
            {
                Route = "custom/route",
                HttpMethod = "PUT",
                Version = "v2"
            };

            // Act
            var result = _resolver.ResolveEndpointConfiguration(typeof(TestHandler), "HandleAsync", attribute);

            // Assert
            Assert.Equal("custom/route", result.Route);
            Assert.Equal("PUT", result.HttpMethod);
            Assert.Equal("v2", result.Version);
        }

        [Fact]
        public void ResolveEndpointConfiguration_AutoRouteGeneration_RemovesHandlerSuffix()
        {
            // Arrange
            _options.DefaultEndpointOptions.EnableAutoRouteGeneration = true;

            // Act
            var result = _resolver.ResolveEndpointConfiguration(typeof(TestRequestHandler), "HandleAsync", null);

            // Assert
            Assert.Equal("testrequest/handleasync", result.Route); // "Handler" suffix removed
        }

        private class TestHandler { }
        private class TestRequestHandler { }
        private class TestPipeline { }
    }
}