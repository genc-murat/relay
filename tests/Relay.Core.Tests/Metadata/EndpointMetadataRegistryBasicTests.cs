using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class EndpointMetadataRegistryBasicTests
    {
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
            Assert.Single(endpoints);
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
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_ReturnsReadOnlyList()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest));

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<EndpointMetadata>>(endpoints);
            Assert.Single(endpoints);
            Assert.Equal(metadata, endpoints.First());
        }

        [Fact]
        public void EndpointMetadataRegistry_AllEndpoints_ReturnsReadOnlyList()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var endpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<EndpointMetadata>>(endpoints);
            Assert.Single(endpoints);
            Assert.Equal(metadata, endpoints.First());
        }

        [Fact]
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_WithNullType_ThrowsArgumentNullException()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EndpointMetadataRegistry.GetEndpointsForRequestType(null!));
        }

        [Fact]
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_WithNonRegisteredType_ReturnsEmpty()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(string));

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_MultipleEndpoints_SameRequestType_GroupedCorrectly()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            var metadata3 = new EndpointMetadata
            {
                Route = "/api/other",
                RequestType = typeof(TestResponse),
                HandlerType = typeof(TestHandler)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            EndpointMetadataRegistry.RegisterEndpoint(metadata3);

            // Act
            var testRequestEndpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest));
            var testResponseEndpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestResponse));
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.Equal(2, testRequestEndpoints.Count);
            Assert.Contains(metadata1, testRequestEndpoints);
            Assert.Contains(metadata2, testRequestEndpoints);

            Assert.Single(testResponseEndpoints);
            Assert.Contains(metadata3, testResponseEndpoints);

            Assert.Equal(3, allEndpoints.Count);
        }

        [Fact]
        public void EndpointMetadataRegistry_RegisterEndpoint_WithComplexMetadata()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/api/complex/{id}",
                HttpMethod = "PUT",
                Version = "v2.1",
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HandlerType = typeof(TestHandler),
                HandlerMethodName = "HandleComplexAsync"
            };
            metadata.Properties["Authorization"] = "Bearer";
            metadata.Properties["CacheDuration"] = 300;

            // Act
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Single(allEndpoints);
            var registered = allEndpoints.First();
            Assert.Equal("/api/complex/{id}", registered.Route);
            Assert.Equal("PUT", registered.HttpMethod);
            Assert.Equal("v2.1", registered.Version);
            Assert.Equal(typeof(TestRequest), registered.RequestType);
            Assert.Equal(typeof(TestResponse), registered.ResponseType);
            Assert.Equal(typeof(TestHandler), registered.HandlerType);
            Assert.Equal("HandleComplexAsync", registered.HandlerMethodName);
            Assert.Equal("Bearer", registered.Properties["Authorization"]);
            Assert.Equal(300, registered.Properties["CacheDuration"]);
        }

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class TestHandler { }
    }
}