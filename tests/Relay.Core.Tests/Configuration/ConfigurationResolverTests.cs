using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Core;
using Relay.Core.Configuration.Options;
using System;
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
            _options.DefaultHandlerOptions.EnableRetry = true;
            _options.DefaultHandlerOptions.MaxRetryAttempts = 5;

            // Act
            var result = _resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", null);

            // Assert
            Assert.Null(result.Name);
            Assert.Equal(5, result.Priority);
            Assert.True(result.EnableCaching);
            Assert.Equal(TimeSpan.FromSeconds(30), result.Timeout);
            Assert.True(result.EnableRetry);
            Assert.Equal(5, result.MaxRetryAttempts);
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
        public void ResolveNotificationConfiguration_WithNullHandlerType_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _resolver.ResolveNotificationConfiguration(null!, "method", null));
        }

        [Fact]
        public void ResolveNotificationConfiguration_WithEmptyMethodName_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _resolver.ResolveNotificationConfiguration(typeof(TestHandler), "", null));
        }

        [Fact]
        public void ResolvePipelineConfiguration_WithNullPipelineType_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _resolver.ResolvePipelineConfiguration(null!, "method", null));
        }

        [Fact]
        public void ResolvePipelineConfiguration_WithEmptyMethodName_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _resolver.ResolvePipelineConfiguration(typeof(TestPipeline), "", null));
        }

        [Fact]
        public void ResolveEndpointConfiguration_WithNullHandlerType_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _resolver.ResolveEndpointConfiguration(null!, "method", null));
        }

        [Fact]
        public void ResolveEndpointConfiguration_WithEmptyMethodName_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _resolver.ResolveEndpointConfiguration(typeof(TestHandler), "", null));
        }

        [Fact]
        public void ResolveNotificationConfiguration_WithNoAttribute_UsesGlobalDefaults()
        {
            // Arrange
            _options.DefaultNotificationOptions.DefaultDispatchMode = NotificationDispatchMode.Sequential;
            _options.DefaultNotificationOptions.DefaultPriority = 3;
            _options.DefaultNotificationOptions.ContinueOnError = false;
            _options.DefaultNotificationOptions.DefaultTimeout = TimeSpan.FromSeconds(45);
            _options.DefaultNotificationOptions.MaxDegreeOfParallelism = 8;

            // Act
            var result = _resolver.ResolveNotificationConfiguration(typeof(TestHandler), "HandleNotification", null);

            // Assert
            Assert.Equal(NotificationDispatchMode.Sequential, result.DispatchMode);
            Assert.Equal(3, result.Priority);
            Assert.False(result.ContinueOnError);
            Assert.Equal(TimeSpan.FromSeconds(45), result.Timeout);
            Assert.Equal(8, result.MaxDegreeOfParallelism);
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
        public void ResolveNotificationConfiguration_WithSpecificOverrides_UsesOverrides()
        {
            // Arrange
            var key = $"{typeof(TestHandler).FullName}.HandleNotification";
            _options.NotificationOverrides[key] = new NotificationOptions
            {
                DefaultDispatchMode = NotificationDispatchMode.Sequential,
                DefaultPriority = 10,
                ContinueOnError = false,
                DefaultTimeout = TimeSpan.FromSeconds(60),
                MaxDegreeOfParallelism = 5
            };

            // Act
            var result = _resolver.ResolveNotificationConfiguration(typeof(TestHandler), "HandleNotification", null);

            // Assert
            Assert.Equal(NotificationDispatchMode.Sequential, result.DispatchMode);
            Assert.Equal(10, result.Priority);
            Assert.False(result.ContinueOnError);
            Assert.Equal(TimeSpan.FromSeconds(60), result.Timeout);
            Assert.Equal(5, result.MaxDegreeOfParallelism);
        }

        [Fact]
        public void ResolveNotificationConfiguration_AttributeOverridesSpecificOverrides()
        {
            // Arrange
            var key = $"{typeof(TestHandler).FullName}.HandleNotification";
            _options.NotificationOverrides[key] = new NotificationOptions { DefaultPriority = 15 };
            var attribute = new NotificationAttribute { Priority = 20 };

            // Act
            var result = _resolver.ResolveNotificationConfiguration(typeof(TestHandler), "HandleNotification", attribute);

            // Assert
            Assert.Equal(20, result.Priority); // Attribute wins
        }

        [Fact]
        public void ResolvePipelineConfiguration_WithNoAttribute_UsesGlobalDefaults()
        {
            // Arrange
            _options.DefaultPipelineOptions.DefaultOrder = 100;
            _options.DefaultPipelineOptions.DefaultScope = PipelineScope.Requests;
            _options.DefaultPipelineOptions.EnableCaching = true;
            _options.DefaultPipelineOptions.DefaultTimeout = TimeSpan.FromMinutes(5);

            // Act
            var result = _resolver.ResolvePipelineConfiguration(typeof(TestPipeline), "Execute", null);

            // Assert
            Assert.Equal(100, result.Order);
            Assert.Equal(PipelineScope.Requests, result.Scope);
            Assert.True(result.EnableCaching);
            Assert.Equal(TimeSpan.FromMinutes(5), result.Timeout);
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
        public void ResolvePipelineConfiguration_WithSpecificOverrides_UsesOverrides()
        {
            // Arrange
            var key = $"{typeof(TestPipeline).FullName}.Execute";
            _options.PipelineOverrides[key] = new PipelineOptions
            {
                DefaultOrder = 200,
                DefaultScope = PipelineScope.Requests,
                EnableCaching = true,
                DefaultTimeout = TimeSpan.FromMinutes(2)
            };

            // Act
            var result = _resolver.ResolvePipelineConfiguration(typeof(TestPipeline), "Execute", null);

            // Assert
            Assert.Equal(200, result.Order);
            Assert.Equal(PipelineScope.Requests, result.Scope);
            Assert.True(result.EnableCaching);
            Assert.Equal(TimeSpan.FromMinutes(2), result.Timeout);
        }

        [Fact]
        public void ResolvePipelineConfiguration_AttributeOverridesSpecificOverrides()
        {
            // Arrange
            var key = $"{typeof(TestPipeline).FullName}.Execute";
            _options.PipelineOverrides[key] = new PipelineOptions { DefaultOrder = 150 };
            var attribute = new PipelineAttribute { Order = 100 };

            // Act
            var result = _resolver.ResolvePipelineConfiguration(typeof(TestPipeline), "Execute", attribute);

            // Assert
            Assert.Equal(100, result.Order); // Attribute wins
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

        [Fact]
        public void ResolveEndpointConfiguration_AutoRouteGeneration_WithoutHandlerSuffix()
        {
            // Arrange
            _options.DefaultEndpointOptions.EnableAutoRouteGeneration = true;

            // Act
            var result = _resolver.ResolveEndpointConfiguration(typeof(TestService), "Process", null);

            // Assert
            Assert.Equal("testservice/process", result.Route); // No "Handler" suffix to remove
        }

        [Fact]
        public void ResolveEndpointConfiguration_AutoRouteGeneration_WithEmptyRoutePrefix()
        {
            // Arrange
            _options.DefaultEndpointOptions.EnableAutoRouteGeneration = true;
            _options.DefaultEndpointOptions.DefaultRoutePrefix = "";

            // Act
            var result = _resolver.ResolveEndpointConfiguration(typeof(TestHandler), "HandleAsync", null);

            // Assert
            Assert.Equal("test/handleasync", result.Route); // No prefix
        }

        [Fact]
        public void ResolveEndpointConfiguration_AutoRouteGeneration_WithNullRoutePrefix()
        {
            // Arrange
            _options.DefaultEndpointOptions.EnableAutoRouteGeneration = true;
            _options.DefaultEndpointOptions.DefaultRoutePrefix = null;

            // Act
            var result = _resolver.ResolveEndpointConfiguration(typeof(TestHandler), "HandleAsync", null);

            // Assert
            Assert.Equal("test/handleasync", result.Route); // No prefix
        }

        [Fact]
        public void ResolveEndpointConfiguration_AutoRouteGeneration_WithRoutePrefixWithoutTrailingSlash()
        {
            // Arrange
            _options.DefaultEndpointOptions.EnableAutoRouteGeneration = true;
            _options.DefaultEndpointOptions.DefaultRoutePrefix = "api/v2";

            // Act
            var result = _resolver.ResolveEndpointConfiguration(typeof(TestHandler), "HandleAsync", null);

            // Assert
            Assert.Equal("api/v2/test/handleasync", result.Route); // Slash added automatically
        }

        [Fact]
        public void ResolveEndpointConfiguration_AutoRouteGeneration_Disabled_UsesNullRoute()
        {
            // Arrange
            _options.DefaultEndpointOptions.EnableAutoRouteGeneration = false;

            // Act
            var result = _resolver.ResolveEndpointConfiguration(typeof(TestHandler), "HandleAsync", null);

            // Assert
            Assert.Null(result.Route);
        }

        [Fact]
        public void ResolveHandlerConfiguration_Precedence_AttributeOverSpecificOverridesOverGlobalDefaults()
        {
            // Arrange
            _options.DefaultHandlerOptions.DefaultPriority = 1;
            _options.DefaultHandlerOptions.EnableCaching = false;
            _options.DefaultHandlerOptions.DefaultTimeout = TimeSpan.FromSeconds(10);

            var key = $"{typeof(TestHandler).FullName}.HandleAsync";
            _options.HandlerOverrides[key] = new HandlerOptions
            {
                DefaultPriority = 2,
                EnableCaching = true,
                DefaultTimeout = TimeSpan.FromSeconds(20)
            };

            var attribute = new HandleAttribute { Priority = 3 };

            // Act
            var result = _resolver.ResolveHandlerConfiguration(typeof(TestHandler), "HandleAsync", attribute);

            // Assert
            Assert.Equal(3, result.Priority); // Attribute wins
            Assert.True(result.EnableCaching); // Specific override wins
            Assert.Equal(TimeSpan.FromSeconds(20), result.Timeout); // Specific override wins
        }

        [Fact]
        public void ResolveNotificationConfiguration_Precedence_AttributeOverSpecificOverridesOverGlobalDefaults()
        {
            // Arrange
            _options.DefaultNotificationOptions.DefaultPriority = 1;
            _options.DefaultNotificationOptions.ContinueOnError = true;
            _options.DefaultNotificationOptions.DefaultTimeout = TimeSpan.FromSeconds(10);

            var key = $"{typeof(TestHandler).FullName}.HandleNotification";
            _options.NotificationOverrides[key] = new NotificationOptions
            {
                DefaultPriority = 2,
                ContinueOnError = false,
                DefaultTimeout = TimeSpan.FromSeconds(20)
            };

            var attribute = new NotificationAttribute { Priority = 3 };

            // Act
            var result = _resolver.ResolveNotificationConfiguration(typeof(TestHandler), "HandleNotification", attribute);

            // Assert
            Assert.Equal(3, result.Priority); // Attribute wins
            Assert.False(result.ContinueOnError); // Specific override wins
            Assert.Equal(TimeSpan.FromSeconds(20), result.Timeout); // Specific override wins
        }

        [Fact]
        public void ResolvePipelineConfiguration_Precedence_AttributeOverSpecificOverridesOverGlobalDefaults()
        {
            // Arrange
            _options.DefaultPipelineOptions.DefaultOrder = 1;
            _options.DefaultPipelineOptions.EnableCaching = false;
            _options.DefaultPipelineOptions.DefaultTimeout = TimeSpan.FromSeconds(10);

            var key = $"{typeof(TestPipeline).FullName}.Execute";
            _options.PipelineOverrides[key] = new PipelineOptions
            {
                DefaultOrder = 2,
                EnableCaching = true,
                DefaultTimeout = TimeSpan.FromSeconds(20)
            };

            var attribute = new PipelineAttribute { Order = 3 };

            // Act
            var result = _resolver.ResolvePipelineConfiguration(typeof(TestPipeline), "Execute", attribute);

            // Assert
            Assert.Equal(3, result.Order); // Attribute wins
            Assert.True(result.EnableCaching); // Specific override wins
            Assert.Equal(TimeSpan.FromSeconds(20), result.Timeout); // Specific override wins
        }

        private class TestHandler { }
        private class TestRequestHandler { }
        private class TestService { }
        private class TestPipeline { }
    }
}