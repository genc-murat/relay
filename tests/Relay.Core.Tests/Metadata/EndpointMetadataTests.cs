using System;
using System.Linq;
using FluentAssertions;
using Relay.Core;
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
            metadata.Route.Should().BeEmpty();
            metadata.HttpMethod.Should().Be("POST");
            metadata.Version.Should().BeNull();
            metadata.HandlerMethodName.Should().BeEmpty();
            metadata.Properties.Should().NotBeNull();
            metadata.Properties.Should().BeEmpty();
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
            metadata.Route.Should().Be("/api/test");
            metadata.HttpMethod.Should().Be("GET");
            metadata.Version.Should().Be("v1");
            metadata.RequestType.Should().Be(typeof(TestRequest));
            metadata.ResponseType.Should().Be(typeof(TestResponse));
            metadata.HandlerType.Should().Be(typeof(TestHandler));
            metadata.HandlerMethodName.Should().Be("HandleAsync");
        }

        [Fact]
        public void JsonSchemaContract_ShouldInitializeWithDefaults()
        {
            // Act
            var contract = new JsonSchemaContract();

            // Assert
            contract.Schema.Should().BeEmpty();
            contract.ContentType.Should().Be("application/json");
            contract.SchemaVersion.Should().Be("http://json-schema.org/draft-07/schema#");
            contract.Properties.Should().NotBeNull();
            contract.Properties.Should().BeEmpty();
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
            contract.Schema.Should().Be("{ \"type\": \"object\" }");
            contract.ContentType.Should().Be("application/xml");
            contract.SchemaVersion.Should().Be("v2.0");
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
            allEndpoints.Should().Contain(metadata);
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
            endpoints.Should().HaveCount(2);
            endpoints.Should().Contain(metadata1);
            endpoints.Should().Contain(metadata2);
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
            endpoints.Should().HaveCount(1);
            endpoints.First().Should().Be(metadata);
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
            allEndpoints.Should().BeEmpty();
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
            allEndpoints.Should().HaveCount(3);
            allEndpoints.Should().Contain(metadata1);
            allEndpoints.Should().Contain(metadata2);
            allEndpoints.Should().Contain(metadata3);
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
            metadata.Properties["Custom1"].Should().Be("Value1");
            metadata.Properties["Custom2"].Should().Be(42);
            metadata.Properties["Custom3"].Should().Be(true);
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
            contract.Properties["required"].Should().BeEquivalentTo(new[] { "name", "age" });
            contract.Properties["additionalProperties"].Should().Be(false);
        }

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class TestHandler { }
    }
}
