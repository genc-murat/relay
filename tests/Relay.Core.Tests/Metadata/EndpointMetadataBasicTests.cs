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
    public class EndpointMetadataBasicTests
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

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class TestHandler { }
    }
}
