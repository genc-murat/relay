using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Metadata.Endpoints;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class EndpointMetadataRegistryPerformanceTests
    {
        [Fact]
        public void EndpointMetadataRegistry_LargeNumberOfEndpoints_Performance()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            const int numEndpoints = 10000;
            var endpoints = new List<EndpointMetadata>();

            for (int i = 0; i < numEndpoints; i++)
            {
                var metadata = new EndpointMetadata
                {
                    Route = $"/api/test{i}",
                    RequestType = typeof(TestRequest),
                    HandlerType = typeof(TestHandler)
                };
                endpoints.Add(metadata);
                EndpointMetadataRegistry.RegisterEndpoint(metadata);
            }

            // Act
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            var requestTypeEndpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest));

            // Assert
            Assert.Equal(numEndpoints, allEndpoints.Count);
            Assert.Equal(numEndpoints, requestTypeEndpoints.Count);
            foreach (var endpoint in endpoints)
            {
                Assert.Contains(endpoint, allEndpoints);
                Assert.Contains(endpoint, requestTypeEndpoints);
            }
        }

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class TestHandler { }
    }
}