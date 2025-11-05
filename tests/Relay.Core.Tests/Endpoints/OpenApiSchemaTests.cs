using System;
using System.Linq;
using System.Text.Json;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Metadata.Endpoints;
using Relay.Core.Metadata.MessageQueue;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Endpoints;

/// <summary>
/// Tests for OpenAPI schema handling functionality
/// </summary>
public class OpenApiSchemaTests
{
    public OpenApiSchemaTests()
    {
        // Clear registry before each test
        EndpointMetadataRegistry.Clear();
    }

    [Fact]
    public void GenerateDocument_WithComplexSchema_CreatesComponentSchemas()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/complex",
            HttpMethod = "POST",
            RequestType = typeof(ComplexRequest),
            ResponseType = typeof(ComplexResponse),
            HandlerType = typeof(OpenApiSchemaTests),
            HandlerMethodName = "HandleComplex",
            RequestSchema = new JsonSchemaContract
            {
                Schema = @"{
                        ""type"": ""object"",
                        ""title"": ""ComplexRequest"",
                        ""properties"": {
                            ""user"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""name"": { ""type"": ""string"" },
                                    ""age"": { ""type"": ""integer"" }
                                }
                            },
                            ""tags"": {
                                ""type"": ""array"",
                                ""items"": { ""type"": ""string"" }
                            }
                        }
                    }"
            },
            ResponseSchema = new JsonSchemaContract
            {
                Schema = @"{
                        ""type"": ""object"",
                        ""title"": ""ComplexResponse"",
                        ""properties"": {
                            ""id"": { ""type"": ""integer"" },
                            ""success"": { ""type"": ""boolean"" }
                        }
                    }"
            }
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Equal(2, document.Components.Schemas.Count);
        Assert.True(document.Components.Schemas.ContainsKey("ComplexRequest"));
        Assert.True(document.Components.Schemas.ContainsKey("ComplexResponse"));

        var requestSchema = document.Components.Schemas["ComplexRequest"];
        Assert.Equal("object", requestSchema.Type);
        Assert.Equal("ComplexRequest", requestSchema.Title);
        Assert.Contains("user", requestSchema.Properties.Keys);
        Assert.Contains("tags", requestSchema.Properties.Keys);

        var responseSchema = document.Components.Schemas["ComplexResponse"];
        Assert.Equal("object", responseSchema.Type);
        Assert.Equal("ComplexResponse", responseSchema.Title);
        Assert.Contains("id", responseSchema.Properties.Keys);
        Assert.Contains("success", responseSchema.Properties.Keys);
    }

    // Test types for OpenAPI schema tests
    public class ComplexRequest : IRequest<ComplexResponse>
    {
        public UserInfo User { get; set; } = new();
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class ComplexResponse
    {
        public int Id { get; set; }
        public bool Success { get; set; }
    }

    public class UserInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
