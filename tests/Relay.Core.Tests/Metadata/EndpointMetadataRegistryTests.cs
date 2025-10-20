using System;
using System.Linq;
using Relay.Core;
using Relay.Core.Metadata.Endpoints;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class EndpointMetadataRegistryTests : IDisposable
    {
        public EndpointMetadataRegistryTests()
        {
            // Clear registry before each test to ensure clean state
            EndpointMetadataRegistry.Clear();
        }

        public void Dispose()
        {
            // Clean up after each test
            EndpointMetadataRegistry.Clear();
        }

        #region EnsureScopeInitialized_NoLock Tests (via RegisterEndpoint)

        [Fact]
        public void EnsureScopeInitialized_NoLock_Should_Create_New_Scope_When_None_Exists()
        {
            // Arrange - No scope exists initially

            // Act - RegisterEndpoint calls EnsureScopeInitialized_NoLock internally
            var metadata = new EndpointMetadata
            {
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "GET",
                Route = "/test"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Assert - Should have created a scope and registered the endpoint
            var endpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Single(endpoints);
            Assert.Equal(metadata, endpoints[0]);
        }

        [Fact]
        public void EnsureScopeInitialized_NoLock_Should_Reuse_Existing_Scope_When_Already_Initialized()
        {
            // Arrange - Register first endpoint to initialize scope
            var metadata1 = new EndpointMetadata
            {
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "GET",
                Route = "/test1"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);

            // Act - Register second endpoint (should reuse existing scope)
            var metadata2 = new EndpointMetadata
            {
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "POST",
                Route = "/test2"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Assert - Both endpoints should be in the same scope
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Equal(2, allEndpoints.Count);
            Assert.Contains(metadata1, allEndpoints);
            Assert.Contains(metadata2, allEndpoints);
        }

        [Fact]
        public void EnsureScopeInitialized_NoLock_Should_Initialize_Dictionaries_For_New_Scope()
        {
            // Arrange - No scope exists

            // Act - Register endpoint which initializes scope
            var metadata = new EndpointMetadata
            {
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "GET",
                Route = "/test"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Assert - Should be able to retrieve by request type
            var endpointsForType = EndpointMetadataRegistry.GetEndpointsForRequestType<TestRequest>();
            Assert.Single(endpointsForType);
            Assert.Equal(metadata, endpointsForType[0]);
        }

        [Fact]
        public void EnsureScopeInitialized_NoLock_Should_Handle_Multiple_Request_Types_In_Same_Scope()
        {
            // Arrange - Register endpoints for different request types
            var metadata1 = new EndpointMetadata
            {
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "GET",
                Route = "/test1"
            };

            var metadata2 = new EndpointMetadata
            {
                RequestType = typeof(AnotherTestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "POST",
                Route = "/test2"
            };

            // Act - Register both endpoints (should be in same scope)
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Assert - Both should be accessible
            var endpoints1 = EndpointMetadataRegistry.GetEndpointsForRequestType<TestRequest>();
            var endpoints2 = EndpointMetadataRegistry.GetEndpointsForRequestType<AnotherTestRequest>();

            Assert.Single(endpoints1);
            Assert.Single(endpoints2);
            Assert.Equal(metadata1, endpoints1[0]);
            Assert.Equal(metadata2, endpoints2[0]);

            // All endpoints should be in AllEndpoints
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Equal(2, allEndpoints.Count);
        }

        [Fact]
        public void EnsureScopeInitialized_NoLock_Should_Preserve_Scope_Across_Multiple_Operations()
        {
            // Arrange - Register first endpoint
            var metadata1 = new EndpointMetadata
            {
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "GET",
                Route = "/test1"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);

            // Act - Register second endpoint for same type
            var metadata2 = new EndpointMetadata
            {
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "POST",
                Route = "/test2"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Assert - Both endpoints should be retrievable for the same request type
            var endpointsForType = EndpointMetadataRegistry.GetEndpointsForRequestType<TestRequest>();
            Assert.Equal(2, endpointsForType.Count);
            Assert.Contains(metadata1, endpointsForType);
            Assert.Contains(metadata2, endpointsForType);
        }

        [Fact]
        public void EnsureScopeInitialized_NoLock_Should_Isolate_Scopes_Between_Clear_Operations()
        {
            // Arrange - Register endpoint in first scope
            var metadata1 = new EndpointMetadata
            {
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "GET",
                Route = "/test1"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);

            // Act - Clear creates new scope
            EndpointMetadataRegistry.Clear();

            // Register endpoint in new scope
            var metadata2 = new EndpointMetadata
            {
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HttpMethod = "POST",
                Route = "/test2"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Assert - Only new endpoint should be visible
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Single(allEndpoints);
            Assert.Equal(metadata2, allEndpoints[0]);
        }

        #endregion

        #region Additional Tests

        [Fact]
        public void RegisterEndpoint_Should_Throw_When_Metadata_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EndpointMetadataRegistry.RegisterEndpoint(null!));
        }

        [Fact]
        public void GetEndpointsForRequestType_Should_Return_Empty_For_Unregistered_Type()
        {
            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(UnregisteredRequest));

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void AllEndpoints_Should_Return_Empty_When_No_Scope_Initialized()
        {
            // Arrange - Clear to ensure no scope
            EndpointMetadataRegistry.Clear();

            // Act - Don't register anything

            // Assert - Should return empty
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Empty(allEndpoints);
        }

        #endregion

        #region Test Classes

        private class TestRequest { }
        private class AnotherTestRequest { }
        private class UnregisteredRequest { }
        private class TestResponse { }

        #endregion
    }
}