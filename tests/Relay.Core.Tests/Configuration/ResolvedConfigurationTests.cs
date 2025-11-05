using System;
using Xunit;
using Relay.Core.Configuration.Resolved;

namespace Relay.Core.Tests.Configuration
{
    public class ResolvedConfigurationTests
    {
        public class ResolvedEndpointConfigurationTests
        {
            [Fact]
            public void ResolvedEndpointConfiguration_DefaultConstructor_ShouldInitializeProperties()
            {
                // Arrange & Act
                var config = new ResolvedEndpointConfiguration();

                // Assert
                Assert.Null(config.Route);
                Assert.Equal("POST", config.HttpMethod);
                Assert.Null(config.Version);
                Assert.False(config.EnableOpenApiGeneration);
                Assert.False(config.EnableAutoRouteGeneration);
            }

            [Fact]
            public void ResolvedEndpointConfiguration_ShouldAllowSettingProperties()
            {
                // Arrange
                var config = new ResolvedEndpointConfiguration();

                // Act
                config.Route = "/api/test";
                config.HttpMethod = "GET";
                config.Version = "v1";
                config.EnableOpenApiGeneration = true;
                config.EnableAutoRouteGeneration = true;

                // Assert
                Assert.Equal("/api/test", config.Route);
                Assert.Equal("GET", config.HttpMethod);
                Assert.Equal("v1", config.Version);
                Assert.True(config.EnableOpenApiGeneration);
                Assert.True(config.EnableAutoRouteGeneration);
            }

            [Fact]
            public void ResolvedEndpointConfiguration_ShouldBeMutable()
            {
                // Arrange
                var config = new ResolvedEndpointConfiguration
                {
                    Route = "/initial",
                    HttpMethod = "POST",
                    Version = "v1",
                    EnableOpenApiGeneration = false,
                    EnableAutoRouteGeneration = false
                };

                // Act
                config.Route = "/updated";
                config.HttpMethod = "PUT";
                config.Version = "v2";
                config.EnableOpenApiGeneration = true;
                config.EnableAutoRouteGeneration = true;

                // Assert
                Assert.Equal("/updated", config.Route);
                Assert.Equal("PUT", config.HttpMethod);
                Assert.Equal("v2", config.Version);
                Assert.True(config.EnableOpenApiGeneration);
                Assert.True(config.EnableAutoRouteGeneration);
            }
        }

        public class ResolvedHandlerConfigurationTests
        {
            [Fact]
            public void ResolvedHandlerConfiguration_DefaultConstructor_ShouldInitializeProperties()
            {
                // Arrange & Act
                var config = new ResolvedHandlerConfiguration();

                // Assert
                Assert.Null(config.Name);
                Assert.Equal(0, config.Priority);
                Assert.False(config.EnableCaching);
                Assert.Null(config.Timeout);
                Assert.False(config.EnableRetry);
                Assert.Equal(0, config.MaxRetryAttempts);
            }

            [Fact]
            public void ResolvedHandlerConfiguration_ShouldAllowSettingProperties()
            {
                // Arrange
                var config = new ResolvedHandlerConfiguration();

                // Act
                config.Name = "TestHandler";
                config.Priority = 100;
                config.EnableCaching = true;
                config.Timeout = TimeSpan.FromSeconds(30);
                config.EnableRetry = true;
                config.MaxRetryAttempts = 3;

                // Assert
                Assert.Equal("TestHandler", config.Name);
                Assert.Equal(100, config.Priority);
                Assert.True(config.EnableCaching);
                Assert.Equal(TimeSpan.FromSeconds(30), config.Timeout);
                Assert.True(config.EnableRetry);
                Assert.Equal(3, config.MaxRetryAttempts);
            }

            [Fact]
            public void ResolvedHandlerConfiguration_ShouldBeMutable()
            {
                // Arrange
                var config = new ResolvedHandlerConfiguration
                {
                    Name = "Initial",
                    Priority = 10,
                    EnableCaching = false,
                    Timeout = TimeSpan.FromMinutes(1),
                    EnableRetry = false,
                    MaxRetryAttempts = 1
                };

                // Act
                config.Name = "Updated";
                config.Priority = 20;
                config.EnableCaching = true;
                config.Timeout = TimeSpan.FromMinutes(2);
                config.EnableRetry = true;
                config.MaxRetryAttempts = 5;

                // Assert
                Assert.Equal("Updated", config.Name);
                Assert.Equal(20, config.Priority);
                Assert.True(config.EnableCaching);
                Assert.Equal(TimeSpan.FromMinutes(2), config.Timeout);
                Assert.True(config.EnableRetry);
                Assert.Equal(5, config.MaxRetryAttempts);
            }
        }

        public class ResolvedNotificationConfigurationTests
        {
            [Fact]
            public void ResolvedNotificationConfiguration_DefaultConstructor_ShouldInitializeProperties()
            {
                // Arrange & Act
                var config = new ResolvedNotificationConfiguration();

                // Assert
                Assert.Equal(default(Relay.Core.NotificationDispatchMode), config.DispatchMode);
                Assert.Equal(0, config.Priority);
                Assert.False(config.ContinueOnError);
                Assert.Null(config.Timeout);
                Assert.Equal(0, config.MaxDegreeOfParallelism);
            }

            [Fact]
            public void ResolvedNotificationConfiguration_ShouldAllowSettingProperties()
            {
                // Arrange
                var config = new ResolvedNotificationConfiguration();

                // Act
                config.DispatchMode = Relay.Core.NotificationDispatchMode.Sequential;
                config.Priority = 50;
                config.ContinueOnError = true;
                config.Timeout = TimeSpan.FromSeconds(10);
                config.MaxDegreeOfParallelism = 4;

                // Assert
                Assert.Equal(Relay.Core.NotificationDispatchMode.Sequential, config.DispatchMode);
                Assert.Equal(50, config.Priority);
                Assert.True(config.ContinueOnError);
                Assert.Equal(TimeSpan.FromSeconds(10), config.Timeout);
                Assert.Equal(4, config.MaxDegreeOfParallelism);
            }

            [Fact]
            public void ResolvedNotificationConfiguration_ShouldBeMutable()
            {
                // Arrange
                var config = new ResolvedNotificationConfiguration
                {
                    DispatchMode = Relay.Core.NotificationDispatchMode.Parallel,
                    Priority = 10,
                    ContinueOnError = false,
                    Timeout = TimeSpan.FromSeconds(5),
                    MaxDegreeOfParallelism = 2
                };

                // Act
                config.DispatchMode = Relay.Core.NotificationDispatchMode.Sequential;
                config.Priority = 20;
                config.ContinueOnError = true;
                config.Timeout = TimeSpan.FromSeconds(15);
                config.MaxDegreeOfParallelism = 8;

                // Assert
                Assert.Equal(Relay.Core.NotificationDispatchMode.Sequential, config.DispatchMode);
                Assert.Equal(20, config.Priority);
                Assert.True(config.ContinueOnError);
                Assert.Equal(TimeSpan.FromSeconds(15), config.Timeout);
                Assert.Equal(8, config.MaxDegreeOfParallelism);
            }
        }

        public class ResolvedPipelineConfigurationTests
        {
            [Fact]
            public void ResolvedPipelineConfiguration_DefaultConstructor_ShouldInitializeProperties()
            {
                // Arrange & Act
                var config = new ResolvedPipelineConfiguration();

                // Assert
                Assert.Equal(0, config.Order);
                Assert.Equal(default(Relay.Core.PipelineScope), config.Scope);
                Assert.False(config.EnableCaching);
                Assert.Null(config.Timeout);
            }

            [Fact]
            public void ResolvedPipelineConfiguration_ShouldAllowSettingProperties()
            {
                // Arrange
                var config = new ResolvedPipelineConfiguration();

                // Act
                config.Order = -100;
                config.Scope = Relay.Core.PipelineScope.Requests;
                config.EnableCaching = true;
                config.Timeout = TimeSpan.FromMinutes(5);

                // Assert
                Assert.Equal(-100, config.Order);
                Assert.Equal(Relay.Core.PipelineScope.Requests, config.Scope);
                Assert.True(config.EnableCaching);
                Assert.Equal(TimeSpan.FromMinutes(5), config.Timeout);
            }

            [Fact]
            public void ResolvedPipelineConfiguration_ShouldBeMutable()
            {
                // Arrange
                var config = new ResolvedPipelineConfiguration
                {
                    Order = 10,
                    Scope = Relay.Core.PipelineScope.All,
                    EnableCaching = false,
                    Timeout = TimeSpan.FromSeconds(30)
                };

                // Act
                config.Order = 20;
                config.Scope = Relay.Core.PipelineScope.Streams;
                config.EnableCaching = true;
                config.Timeout = TimeSpan.FromMinutes(1);

                // Assert
                Assert.Equal(20, config.Order);
                Assert.Equal(Relay.Core.PipelineScope.Streams, config.Scope);
                Assert.True(config.EnableCaching);
                Assert.Equal(TimeSpan.FromMinutes(1), config.Timeout);
            }
        }
    }
}

