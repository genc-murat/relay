using System;
using System.Linq;
using System.Text.Json;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.Endpoints;

/// <summary>
/// Tests for OpenAPI document generation with all HTTP methods
/// </summary>
public class OpenApiHttpMethodsTests
{
    public OpenApiHttpMethodsTests()
    {
        // Clear registry before each test
        EndpointMetadataRegistry.Clear();
    }

    [Fact]
    public void GenerateDocument_WithAllHttpMethods_CoversAllMethodBranches()
    {
        // Arrange
        var endpoints = new[]
        {
            new EndpointMetadata { Route = "/api/test", HttpMethod = "GET", RequestType = typeof(string), HandlerType = typeof(OpenApiHttpMethodsTests), HandlerMethodName = "GetMethod" },
            new EndpointMetadata { Route = "/api/test", HttpMethod = "POST", RequestType = typeof(string), HandlerType = typeof(OpenApiHttpMethodsTests), HandlerMethodName = "PostMethod" },
            new EndpointMetadata { Route = "/api/test", HttpMethod = "PUT", RequestType = typeof(string), HandlerType = typeof(OpenApiHttpMethodsTests), HandlerMethodName = "PutMethod" },
            new EndpointMetadata { Route = "/api/test", HttpMethod = "DELETE", RequestType = typeof(string), HandlerType = typeof(OpenApiHttpMethodsTests), HandlerMethodName = "DeleteMethod" },
            new EndpointMetadata { Route = "/api/test", HttpMethod = "PATCH", RequestType = typeof(string), HandlerType = typeof(OpenApiHttpMethodsTests), HandlerMethodName = "PatchMethod" },
            new EndpointMetadata { Route = "/api/test", HttpMethod = "HEAD", RequestType = typeof(string), HandlerType = typeof(OpenApiHttpMethodsTests), HandlerMethodName = "HeadMethod" },
            new EndpointMetadata { Route = "/api/test", HttpMethod = "OPTIONS", RequestType = typeof(string), HandlerType = typeof(OpenApiHttpMethodsTests), HandlerMethodName = "OptionsMethod" },
            new EndpointMetadata { Route = "/api/test", HttpMethod = "TRACE", RequestType = typeof(string), HandlerType = typeof(OpenApiHttpMethodsTests), HandlerMethodName = "TraceMethod" }
        };

        foreach (var endpoint in endpoints)
        {
            EndpointMetadataRegistry.RegisterEndpoint(endpoint);
        }

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Single(document.Paths);
        var pathItem = document.Paths["/api/test"];

        Assert.NotNull(pathItem.Get);
        Assert.NotNull(pathItem.Post);
        Assert.NotNull(pathItem.Put);
        Assert.NotNull(pathItem.Delete);
        Assert.NotNull(pathItem.Patch);
        Assert.NotNull(pathItem.Head);
        Assert.NotNull(pathItem.Options);
        Assert.NotNull(pathItem.Trace);
    }
}