using System;
using System.Linq;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class EndpointMetadataTests
    {
        [Fact]
        public void EndpointMetadata_ShouldInitializeWithDefaults()
        {
            // Act
            var metadata = new EndpointMetadata();

            // Assert
            Assert.Empty(metadata.Route);
            Assert.Equal("POST", metadata.HttpMethod);
            Assert.Null(metadata.Version);
            Assert.Empty(metadata.HandlerMethodName);
            Assert.NotNull(metadata.Properties);
            Assert.Empty(metadata.Properties);
        }

        [Fact]
        public void EndpointMetadata_ShouldAllowSettingProperties()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "GET",
                Version = "v1",
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HandlerType = typeof(TestHandler),
                HandlerMethodName = "HandleAsync"
            };

            // Assert
            Assert.Equal("/api/test", metadata.Route);
            Assert.Equal("GET", metadata.HttpMethod);
            Assert.Equal("v1", metadata.Version);
            Assert.Equal(typeof(TestRequest), metadata.RequestType);
            Assert.Equal(typeof(TestResponse), metadata.ResponseType);
            Assert.Equal(typeof(TestHandler), metadata.HandlerType);
            Assert.Equal("HandleAsync", metadata.HandlerMethodName);
        }

        [Fact]
        public void JsonSchemaContract_ShouldInitializeWithDefaults()
        {
            // Act
            var contract = new JsonSchemaContract();

            // Assert
            Assert.Empty(contract.Schema);
            Assert.Equal("application/json", contract.ContentType);
            Assert.Equal("http://json-schema.org/draft-07/schema#", contract.SchemaVersion);
            Assert.NotNull(contract.Properties);
            Assert.Empty(contract.Properties);
        }

        [Fact]
        public void JsonSchemaContract_ShouldAllowSettingProperties()
        {
            // Arrange
            var contract = new JsonSchemaContract
            {
                Schema = "{ \"type\": \"object\" }",
                ContentType = "application/xml",
                SchemaVersion = "v2.0"
            };

            // Assert
            Assert.Equal("{ \"type\": \"object\" }", contract.Schema);
            Assert.Equal("application/xml", contract.ContentType);
            Assert.Equal("v2.0", contract.SchemaVersion);
        }

        [Fact]
        public void EndpointMetadataRegistry_RegisterEndpoint_ShouldAddToRegistry()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            // Act
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Contains(metadata, allEndpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_ShouldReturnMatchingEndpoints()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata
            {
                Route = "/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            var metadata2 = new EndpointMetadata
            {
                Route = "/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest));

            // Assert
            Assert.Equal(2, endpoints.Count());
            Assert.Contains(metadata1, endpoints);
            Assert.Contains(metadata2, endpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_Generic_ShouldWork()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType<TestRequest>();

            // Assert
            Assert.Equal(1, endpoints.Count());
            Assert.Equal(metadata, endpoints.First());
        }

        [Fact]
        public void EndpointMetadataRegistry_Clear_ShouldRemoveAllEndpoints()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            EndpointMetadataRegistry.Clear();

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Empty(allEndpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_AllEndpoints_ShouldReturnAllRegistered()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata { Route = "/test1", RequestType = typeof(TestRequest), HandlerType = typeof(TestHandler) };
            var metadata2 = new EndpointMetadata { Route = "/test2", RequestType = typeof(TestResponse), HandlerType = typeof(TestHandler) };
            var metadata3 = new EndpointMetadata { Route = "/test3", RequestType = typeof(TestRequest), HandlerType = typeof(TestHandler) };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            EndpointMetadataRegistry.RegisterEndpoint(metadata3);

            // Act
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.Equal(3, allEndpoints.Count());
            Assert.Contains(metadata1, allEndpoints);
            Assert.Contains(metadata2, allEndpoints);
            Assert.Contains(metadata3, allEndpoints);
        }

        [Fact]
        public void EndpointMetadata_Properties_CanStoreCustomData()
        {
            // Arrange
            var metadata = new EndpointMetadata();

            // Act
            metadata.Properties["Custom1"] = "Value1";
            metadata.Properties["Custom2"] = 42;
            metadata.Properties["Custom3"] = true;

            // Assert
            Assert.Equal("Value1", metadata.Properties["Custom1"]);
            Assert.Equal(42, metadata.Properties["Custom2"]);
            Assert.Equal(true, metadata.Properties["Custom3"]);
        }

        [Fact]
        public void JsonSchemaContract_Properties_CanStoreCustomData()
        {
            // Arrange
            var contract = new JsonSchemaContract();

            // Act
            contract.Properties["required"] = new[] { "name", "age" };
            contract.Properties["additionalProperties"] = false;

            // Assert
            Assert.Equal(new[] { "name", "age" }, contract.Properties["required"]);
            Assert.Equal(false, contract.Properties["additionalProperties"]);
        }

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class TestHandler { }
    }
}