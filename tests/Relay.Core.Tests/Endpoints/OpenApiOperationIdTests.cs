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
/// Tests for OpenAPI operation ID generation functionality
/// </summary>
public class OpenApiOperationIdTests
{
    public OpenApiOperationIdTests()
    {
        // Clear registry before each test
        EndpointMetadataRegistry.Clear();
    }

    [Fact]
    public void GenerateOperationId_WithCommandAndQuerySuffixes_RemovesSuffixes()
    {
        // Arrange
        var commandMetadata = new EndpointMetadata
        {
            Route = "/api/command",
            HttpMethod = "POST",
            RequestType = typeof(OpenApiTestCommand),
            HandlerType = typeof(OpenApiOperationIdTests),
            HandlerMethodName = "HandleCommand"
        };

        var queryMetadata = new EndpointMetadata
        {
            Route = "/api/query",
            HttpMethod = "GET",
            RequestType = typeof(OpenApiTestQuery),
            HandlerType = typeof(OpenApiOperationIdTests),
            HandlerMethodName = "HandleQuery"
        };

        EndpointMetadataRegistry.RegisterEndpoint(commandMetadata);
        EndpointMetadataRegistry.RegisterEndpoint(queryMetadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Equal(2, document.Paths.Count);
        var commandPath = document.Paths["/api/command"];
        var queryPath = document.Paths["/api/query"];

        Assert.Equal("postOpenApiTest", commandPath.Post.OperationId);
        Assert.Equal("getOpenApiTest", queryPath.Get.OperationId);
    }

    // Test types for OpenAPI operation ID tests
    public class OpenApiTestCommand : IRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class OpenApiTestQuery : IRequest<string>
    {
        public int Id { get; set; }
    }
}
