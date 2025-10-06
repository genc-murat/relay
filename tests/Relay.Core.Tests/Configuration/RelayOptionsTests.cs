using System;
using System.Collections.Generic;
using Relay.Core.Configuration;
using Relay.Core.Configuration.Options;
using Xunit;

namespace Relay.Core.Tests.Configuration
{
    public class RelayOptionsTests
    {
        [Fact]
        public void RelayOptions_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var options = new RelayOptions();

            // Assert
            Assert.NotNull(options.DefaultHandlerOptions);
            Assert.NotNull(options.DefaultNotificationOptions);
            Assert.NotNull(options.DefaultPipelineOptions);
            Assert.NotNull(options.DefaultEndpointOptions);
            Assert.True(options.EnableTelemetry);
            Assert.True(options.EnablePerformanceOptimizations);
            Assert.True(options.MaxConcurrentNotificationHandlers > 0);
            Assert.NotNull(options.HandlerOverrides);
            Assert.NotNull(options.NotificationOverrides);
            Assert.NotNull(options.PipelineOverrides);
        }

        [Fact]
        public void HandlerOptions_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var options = new HandlerOptions();

            // Assert
            Assert.Equal(0, options.DefaultPriority);
            Assert.False(options.EnableCaching);
            Assert.Null(options.DefaultTimeout);
            Assert.False(options.EnableRetry);
            Assert.Equal(3, options.MaxRetryAttempts);
        }

        [Fact]
        public void NotificationOptions_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var options = new NotificationOptions();

            // Assert
            Assert.Equal(NotificationDispatchMode.Parallel, options.DefaultDispatchMode);
            Assert.Equal(0, options.DefaultPriority);
            Assert.True(options.ContinueOnError);
            Assert.Null(options.DefaultTimeout);
            Assert.True(options.MaxDegreeOfParallelism > 0);
        }

        [Fact]
        public void PipelineOptions_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var options = new PipelineOptions();

            // Assert
            Assert.Equal(0, options.DefaultOrder);
            Assert.Equal(PipelineScope.All, options.DefaultScope);
            Assert.False(options.EnableCaching);
            Assert.Null(options.DefaultTimeout);
        }

        [Fact]
        public void EndpointOptions_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var options = new EndpointOptions();

            // Assert
            Assert.Equal("POST", options.DefaultHttpMethod);
            Assert.Null(options.DefaultRoutePrefix);
            Assert.Null(options.DefaultVersion);
            Assert.True(options.EnableOpenApiGeneration);
            Assert.True(options.EnableAutoRouteGeneration);
        }

        [Fact]
        public void RelayOptions_CanSetCustomValues()
        {
            // Arrange
            var options = new RelayOptions
            {
                EnableTelemetry = false,
                EnablePerformanceOptimizations = false,
                MaxConcurrentNotificationHandlers = 10
            };

            options.DefaultHandlerOptions.DefaultPriority = 5;
            options.DefaultNotificationOptions.DefaultDispatchMode = NotificationDispatchMode.Sequential;
            options.DefaultPipelineOptions.DefaultOrder = 100;
            options.DefaultEndpointOptions.DefaultHttpMethod = "GET";

            // Act & Assert
            Assert.False(options.EnableTelemetry);
            Assert.False(options.EnablePerformanceOptimizations);
            Assert.Equal(10, options.MaxConcurrentNotificationHandlers);
            Assert.Equal(5, options.DefaultHandlerOptions.DefaultPriority);
            Assert.Equal(NotificationDispatchMode.Sequential, options.DefaultNotificationOptions.DefaultDispatchMode);
            Assert.Equal(100, options.DefaultPipelineOptions.DefaultOrder);
            Assert.Equal("GET", options.DefaultEndpointOptions.DefaultHttpMethod);
        }

        [Fact]
        public void RelayOptions_CanAddOverrides()
        {
            // Arrange
            var options = new RelayOptions();
            var handlerKey = "TestHandler.HandleAsync";
            var notificationKey = "TestHandler.HandleNotification";
            var pipelineKey = "TestPipeline.Execute";

            // Act
            options.HandlerOverrides[handlerKey] = new HandlerOptions { DefaultPriority = 10 };
            options.NotificationOverrides[notificationKey] = new NotificationOptions { DefaultPriority = 20 };
            options.PipelineOverrides[pipelineKey] = new PipelineOptions { DefaultOrder = 30 };

            // Assert
            Assert.Contains(handlerKey, options.HandlerOverrides.Keys);
            Assert.Contains(notificationKey, options.NotificationOverrides.Keys);
            Assert.Contains(pipelineKey, options.PipelineOverrides.Keys);
            Assert.Equal(10, options.HandlerOverrides[handlerKey].DefaultPriority);
            Assert.Equal(20, options.NotificationOverrides[notificationKey].DefaultPriority);
            Assert.Equal(30, options.PipelineOverrides[pipelineKey].DefaultOrder);
        }
    }
}