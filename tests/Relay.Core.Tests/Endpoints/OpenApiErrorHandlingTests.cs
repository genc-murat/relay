using System;
using System.Linq;
using System.Text.Json;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Metadata.Endpoints;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Endpoints;

/// <summary>
/// Tests for OpenAPI error handling and edge cases
/// </summary>
public class OpenApiErrorHandlingTests
{
    public OpenApiErrorHandlingTests()
    {
        // Clear registry before each test
        EndpointMetadataRegistry.Clear();
    }

    [Fact]
    public void ConvertJsonSchemaToOpenApiSchema_WithInvalidJson_ReturnsDefaultSchema()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var schema = typeof(OpenApiDocumentGenerator)
            .GetMethod("ConvertJsonSchemaToOpenApiSchema", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, new object[] { invalidJson }) as OpenApiSchema;

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("object", schema.Type);
    }

    [Fact]
    public void GenerateDocument_WithNullOptions_UsesDefaultOptions()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "GET",
            RequestType = typeof(string),
            HandlerType = typeof(OpenApiErrorHandlingTests),
            HandlerMethodName = "TestMethod"
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument((OpenApiGenerationOptions)null!);

        // Assert
        Assert.Equal("3.0.1", document.OpenApi);
        Assert.Equal("Relay API", document.Info.Title);
        Assert.Equal("1.0.0", document.Info.Version);
        Assert.Single(document.Servers);
    }
}