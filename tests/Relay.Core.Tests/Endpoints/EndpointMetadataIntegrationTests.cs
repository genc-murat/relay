using System;
using System.Linq;
using Xunit;
using Relay.Core;

namespace Relay.Core.Tests
{
    public class EndpointMetadataIntegrationTests
    {
        public EndpointMetadataIntegrationTests()
        {
            // Clear registry before each test
            EndpointMetadataRegistry.Clear();
        }

        [Fact]
        public void EndpointMetadata_CanBeRegistered_AndRetrieved()
        {
            // Arrange
            var requestType = typeof(TestEndpointRequest);
            var responseType = typeof(TestEndpointResponse);
            var handlerType = typeof(TestEndpointHandler);

            var metadata = new EndpointMetadata
            {
                Route = "/api/test-endpoint",
                HttpMethod = "POST",
                Version = "v1",
                RequestType = requestType,
                ResponseType = responseType,
                HandlerType = handlerType,
                HandlerMethodName = "HandleAsync",
                RequestSchema = new JsonSchemaContract
                {
                    Schema = @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""name"": { ""type"": ""string"" },
                            ""value"": { ""type"": ""integer"" }
                        },
                        ""required"": [""name""]
                    }",
                    ContentType = "application/json"
                },
                ResponseSchema = new JsonSchemaContract
                {
                    Schema = @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""id"": { ""type"": ""integer"" },
                            ""result"": { ""type"": ""string"" }
                        }
                    }",
                    ContentType = "application/json"
                }
            };

            // Act
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Single(allEndpoints);

            var retrievedMetadata = allEndpoints.First();
            Assert.Equal("/api/test-endpoint", retrievedMetadata.Route);
            Assert.Equal("POST", retrievedMetadata.HttpMethod);
            Assert.Equal("v1", retrievedMetadata.Version);
            Assert.Equal(requestType, retrievedMetadata.RequestType);
            Assert.Equal(responseType, retrievedMetadata.ResponseType);
            Assert.Equal(handlerType, retrievedMetadata.HandlerType);
            Assert.Equal("HandleAsync", retrievedMetadata.HandlerMethodName);
            Assert.NotNull(retrievedMetadata.RequestSchema);
            Assert.NotNull(retrievedMetadata.ResponseSchema);
        }

        [Fact]
        public void EndpointMetadata_CanBeRetrieved_ByRequestType()
        {
            // Arrange
            var requestType1 = typeof(TestEndpointRequest);
            var requestType2 = typeof(AnotherTestRequest);

            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = requestType1,
                HandlerType = typeof(TestEndpointHandler)
            };

            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = requestType2,
                HandlerType = typeof(TestEndpointHandler)
            };

            var metadata3 = new EndpointMetadata
            {
                Route = "/api/test3",
                RequestType = requestType1, // Same as metadata1
                HandlerType = typeof(TestEndpointHandler)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            EndpointMetadataRegistry.RegisterEndpoint(metadata3);

            // Act
            var endpointsForType1 = EndpointMetadataRegistry.GetEndpointsForRequestType(requestType1);
            var endpointsForType2 = EndpointMetadataRegistry.GetEndpointsForRequestType(requestType2);

            // Assert
            Assert.Equal(2, endpointsForType1.Count);
            Assert.Contains(metadata1, endpointsForType1);
            Assert.Contains(metadata3, endpointsForType1);

            Assert.Single(endpointsForType2);
            Assert.Contains(metadata2, endpointsForType2);
        }

        [Fact]
        public void EndpointMetadata_WithComplexJsonSchema_CanBeStored()
        {
            // Arrange
            var complexSchema = @"{
                ""$schema"": ""http://json-schema.org/draft-07/schema#"",
                ""type"": ""object"",
                ""title"": ""ComplexRequest"",
                ""properties"": {
                    ""user"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""name"": { ""type"": ""string"" },
                            ""email"": { ""type"": ""string"", ""format"": ""email"" },
                            ""age"": { ""type"": ""integer"", ""minimum"": 0 }
                        },
                        ""required"": [""name"", ""email""]
                    },
                    ""tags"": {
                        ""type"": ""array"",
                        ""items"": { ""type"": ""string"" }
                    },
                    ""metadata"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    }
                },
                ""required"": [""user""]
            }";

            var metadata = new EndpointMetadata
            {
                Route = "/api/complex",
                RequestType = typeof(ComplexTestRequest),
                HandlerType = typeof(TestEndpointHandler),
                RequestSchema = new JsonSchemaContract
                {
                    Schema = complexSchema,
                    ContentType = "application/json",
                    SchemaVersion = "http://json-schema.org/draft-07/schema#"
                }
            };

            // Act
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Assert
            var retrievedMetadata = EndpointMetadataRegistry.AllEndpoints.First();
            Assert.NotNull(retrievedMetadata.RequestSchema);
            Assert.Equal(complexSchema, retrievedMetadata.RequestSchema.Schema);
            Assert.Contains("ComplexRequest", retrievedMetadata.RequestSchema.Schema);
            Assert.Contains("email", retrievedMetadata.RequestSchema.Schema);
        }
    }

    // Test types for endpoint metadata tests
    public class TestEndpointRequest : IRequest<TestEndpointResponse>
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class TestEndpointResponse
    {
        public int Id { get; set; }
        public string Result { get; set; } = string.Empty;
    }

    public class AnotherTestRequest : IRequest<string>
    {
        public string Data { get; set; } = string.Empty;
    }

    public class ComplexTestRequest : IRequest<string>
    {
        public UserInfo User { get; set; } = new();
        public string[] Tags { get; set; } = Array.Empty<string>();
        public object? Metadata { get; set; }
    }

    public class UserInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class TestEndpointHandler
    {
        [Handle]
        [ExposeAsEndpoint(Route = "/api/test-endpoint", HttpMethod = "POST", Version = "v1")]
        public TestEndpointResponse HandleAsync(TestEndpointRequest request)
        {
            return new TestEndpointResponse
            {
                Id = 1,
                Result = $"Processed: {request.Name}"
            };
        }
    }
}