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
/// Tests for OpenAPI document JSON serialization functionality
/// </summary>
public class OpenApiSerializationTests
{
    public OpenApiSerializationTests()
    {
        // Clear registry before each test
        EndpointMetadataRegistry.Clear();
    }

    [Fact]
    public void SerializeToJson_WithValidDocument_ProducesValidJson()
    {
        // Arrange
        EndpointMetadataRegistry.Clear();
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "GET",
            RequestType = typeof(string),
            ResponseType = typeof(string),
            HandlerType = typeof(OpenApiSerializationTests),
            HandlerMethodName = "TestMethod"
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Act
        var json = OpenApiDocumentGenerator.SerializeToJson(document);

        // Assert
        Assert.NotEmpty(json);

        // Verify it's valid JSON
        var parsedDocument = JsonDocument.Parse(json);
        Assert.NotNull(parsedDocument);

        // Verify basic structure
        var root = parsedDocument.RootElement;
        // OpenAPI property might be serialized as "openApi" due to camelCase policy
        var hasOpenApiProperty = root.TryGetProperty("openapi", out var openApiProperty) ||
                                root.TryGetProperty("openApi", out openApiProperty);
        Assert.True(hasOpenApiProperty, "Missing 'openapi' or 'openApi' property in JSON");
        Assert.Equal("3.0.1", openApiProperty.GetString());

        Assert.True(root.TryGetProperty("info", out var infoProperty));
        Assert.True(infoProperty.TryGetProperty("title", out var titleProperty));
        Assert.Equal("Relay API", titleProperty.GetString());

        Assert.True(root.TryGetProperty("paths", out var pathsProperty));
        Assert.True(pathsProperty.TryGetProperty("/api/test", out _));
    }
}