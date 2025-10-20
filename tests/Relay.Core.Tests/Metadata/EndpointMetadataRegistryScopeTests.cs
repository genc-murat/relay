using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class EndpointMetadataRegistryScopeTests
    {
        [Fact]
        public async Task EndpointMetadataRegistry_AsyncLocal_ScopeSharing()
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

            // Act - Register in main context
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            var mainContextEndpoints = EndpointMetadataRegistry.AllEndpoints;

            // Register in async context (Task.Run shares AsyncLocal with parent)
            await Task.Run(() =>
            {
                EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            });

            var afterAsyncEndpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert - AsyncLocal flows to Task.Run, so same scope is shared
            Assert.Single(mainContextEndpoints);
            Assert.Equal(metadata1, mainContextEndpoints.First());
            // After async operation, main context sees both endpoints (same scope)
            Assert.Equal(2, afterAsyncEndpoints.Count);
            Assert.Contains(metadata1, afterAsyncEndpoints);
            Assert.Contains(metadata2, afterAsyncEndpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_ScopeIsolation_BetweenDifferentScopes()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            // Act - Register in first scope
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            var endpointsInScope1 = EndpointMetadataRegistry.AllEndpoints;

            // Clear to create new scope
            EndpointMetadataRegistry.Clear();
            var endpointsAfterClear = EndpointMetadataRegistry.AllEndpoints;

            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            var endpointsInScope2 = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.Single(endpointsInScope1);
            Assert.Equal(metadata1, endpointsInScope1.First());
            Assert.Empty(endpointsAfterClear);
            Assert.Single(endpointsInScope2);
            Assert.Equal(metadata2, endpointsInScope2.First());
        }

        [Fact]
        public void EndpointMetadataRegistry_ScopeManagement_EnsureScopeInitialized_NoLock()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();

            // Act - First call should initialize scope
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);

            // Register another in same scope
            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Equal(2, allEndpoints.Count);
            Assert.Contains(metadata1, allEndpoints);
            Assert.Contains(metadata2, allEndpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_Clear_ResetsScopeCompletely()
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
                RequestType = typeof(TestResponse),
                HandlerType = typeof(TestHandler)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Act
            EndpointMetadataRegistry.Clear();

            // Assert
            Assert.Empty(EndpointMetadataRegistry.AllEndpoints);
            Assert.Empty(EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest)));
            Assert.Empty(EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestResponse)));
        }

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class TestHandler { }
    }
}