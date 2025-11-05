using System;
using System.Linq;
using Xunit;
using Relay.Core;
using Relay.Core.Metadata.MessageQueue;
using Relay.Core.Metadata.Endpoints;

namespace Relay.Core.Tests
{
    public class EndpointMetadataTests
    {
        [Fact]
        public void EndpointMetadata_CanBeCreated_WithAllProperties()
        {
            // Arrange & Act
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "GET",
                Version = "v1",
                RequestType = typeof(string),
                ResponseType = typeof(int),
                HandlerType = typeof(EndpointMetadataTests),
                HandlerMethodName = "TestMethod",
                RequestSchema = new JsonSchemaContract
                {
                    Schema = "{ \"type\": \"string\" }",
                    ContentType = "application/json"
                },
                ResponseSchema = new JsonSchemaContract
                {
                    Schema = "{ \"type\": \"integer\" }",
                    ContentType = "application/json"
                }
            };

            // Assert
            Assert.Equal("/api/test", metadata.Route);
            Assert.Equal("GET", metadata.HttpMethod);
            Assert.Equal("v1", metadata.Version);
            Assert.Equal(typeof(string), metadata.RequestType);
            Assert.Equal(typeof(int), metadata.ResponseType);
            Assert.Equal(typeof(EndpointMetadataTests), metadata.HandlerType);
            Assert.Equal("TestMethod", metadata.HandlerMethodName);
            Assert.NotNull(metadata.RequestSchema);
            Assert.NotNull(metadata.ResponseSchema);
            Assert.NotNull(metadata.Properties);
        }

        [Fact]
        public void JsonSchemaContract_CanBeCreated_WithDefaultValues()
        {
            // Arrange & Act
            var contract = new JsonSchemaContract();

            // Assert
            Assert.Equal(string.Empty, contract.Schema);
            Assert.Equal("application/json", contract.ContentType);
            Assert.Equal("http://json-schema.org/draft-07/schema#", contract.SchemaVersion);
            Assert.NotNull(contract.Properties);
        }

        [Fact]
        public void JsonSchemaContract_CanBeCreated_WithCustomValues()
        {
            // Arrange & Act
            var contract = new JsonSchemaContract
            {
                Schema = "{ \"type\": \"object\" }",
                ContentType = "application/xml",
                SchemaVersion = "http://json-schema.org/draft-04/schema#"
            };

            // Assert
            Assert.Equal("{ \"type\": \"object\" }", contract.Schema);
            Assert.Equal("application/xml", contract.ContentType);
            Assert.Equal("http://json-schema.org/draft-04/schema#", contract.SchemaVersion);
        }
    }

    public class EndpointMetadataRegistryTests
    {
        public EndpointMetadataRegistryTests()
        {
            // Clear registry before each test
            EndpointMetadataRegistry.Clear();
        }

        [Fact]
        public void RegisterEndpoint_AddsEndpointToRegistry()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(string),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            // Act
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Single(allEndpoints);
            Assert.Equal(metadata, allEndpoints.First());
        }

        [Fact]
        public void RegisterEndpoint_WithMultipleEndpoints_AddsAllToRegistry()
        {
            // Arrange
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(string),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(int),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            // Act
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Equal(2, allEndpoints.Count);
            Assert.Contains(metadata1, allEndpoints);
            Assert.Contains(metadata2, allEndpoints);
        }

        [Fact]
        public void GetEndpointsForRequestType_WithRegisteredType_ReturnsEndpoints()
        {
            // Arrange
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(string),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(string),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            var metadata3 = new EndpointMetadata
            {
                Route = "/api/test3",
                RequestType = typeof(int),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            EndpointMetadataRegistry.RegisterEndpoint(metadata3);

            // Act
            var stringEndpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(string));
            var intEndpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(int));

            // Assert
            Assert.Equal(2, stringEndpoints.Count);
            Assert.Contains(metadata1, stringEndpoints);
            Assert.Contains(metadata2, stringEndpoints);

            Assert.Single(intEndpoints);
            Assert.Contains(metadata3, intEndpoints);
        }

        [Fact]
        public void GetEndpointsForRequestType_WithUnregisteredType_ReturnsEmpty()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(string),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(int));

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void GetEndpointsForRequestType_Generic_WithRegisteredType_ReturnsEndpoints()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(string),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType<string>();

            // Assert
            Assert.Single(endpoints);
            Assert.Contains(metadata, endpoints);
        }

        [Fact]
        public void Clear_RemovesAllEndpoints()
        {
            // Arrange
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(string),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(int),
                HandlerType = typeof(EndpointMetadataRegistryTests)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Act
            EndpointMetadataRegistry.Clear();

            // Assert
            Assert.Empty(EndpointMetadataRegistry.AllEndpoints);
            Assert.Empty(EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(string)));
            Assert.Empty(EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(int)));
        }

        [Fact]
        public void AllEndpoints_ReturnsEmpty_WhenNoScopeInitialized()
        {
            // Arrange - No setup, registry should be uninitialized

            // Act
            var endpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void GetEndpointsForRequestType_ReturnsEmpty_WhenNoScopeInitialized()
        {
            // Arrange - No setup, registry should be uninitialized

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(string));

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void GetEndpointsForRequestType_Generic_ReturnsEmpty_WhenNoScopeInitialized()
        {
            // Arrange - No setup, registry should be uninitialized

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType<string>();

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void RegisterEndpoint_ThrowsArgumentNullException_WhenMetadataIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => EndpointMetadataRegistry.RegisterEndpoint(null!));
        }
    }

    public class EndpointMetadataRegistryUninitializedTests
    {
        [Fact]
        public void AllEndpoints_ReturnsEmpty_WhenNoScopeInitialized()
        {
            // Arrange - No setup, registry should be uninitialized

            // Act
            var endpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void GetEndpointsForRequestType_ReturnsEmpty_WhenNoScopeInitialized()
        {
            // Arrange - No setup, registry should be uninitialized

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(string));

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void GetEndpointsForRequestType_Generic_ReturnsEmpty_WhenNoScopeInitialized()
        {
            // Arrange - No setup, registry should be uninitialized

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType<string>();

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void RegisterEndpoint_InitializesScope_WhenNoScopeExists()
        {
            // Arrange - Ensure registry is cleared first
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(string),
                HandlerType = typeof(EndpointMetadataRegistryUninitializedTests)
            };

            // Act
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Single(allEndpoints);
            Assert.Equal(metadata, allEndpoints.First());
        }
    }
}
