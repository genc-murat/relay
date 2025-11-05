using System.Collections.Generic;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiComponents class
/// </summary>
public class OpenApiComponentsTests
{
    [Fact]
    public void OpenApiComponents_DefaultConstructor_InitializesProperties()
    {
        // Act
        var components = new OpenApiComponents();

        // Assert
        Assert.NotNull(components.Schemas);
        Assert.Empty(components.Schemas);
        Assert.NotNull(components.Responses);
        Assert.Empty(components.Responses);
        Assert.NotNull(components.Parameters);
        Assert.Empty(components.Parameters);
        Assert.NotNull(components.Examples);
        Assert.Empty(components.Examples);
        Assert.NotNull(components.RequestBodies);
        Assert.Empty(components.RequestBodies);
        Assert.NotNull(components.Headers);
        Assert.Empty(components.Headers);
    }

    [Fact]
    public void OpenApiComponents_CanAddSchemas()
    {
        // Arrange
        var components = new OpenApiComponents();
        var schema = new OpenApiSchema { Type = "object" };

        // Act
        components.Schemas["User"] = schema;

        // Assert
        Assert.Single(components.Schemas);
        Assert.Contains("User", components.Schemas.Keys);
        Assert.Equal("object", components.Schemas["User"].Type);
    }

    [Fact]
    public void OpenApiComponents_CanAddResponses()
    {
        // Arrange
        var components = new OpenApiComponents();
        var response = new OpenApiResponse { Description = "Not Found" };

        // Act
        components.Responses["NotFound"] = response;

        // Assert
        Assert.Single(components.Responses);
        Assert.Contains("NotFound", components.Responses.Keys);
        Assert.Equal("Not Found", components.Responses["NotFound"].Description);
    }

    [Fact]
    public void OpenApiComponents_CanAddParameters()
    {
        // Arrange
        var components = new OpenApiComponents();
        var parameter = new OpenApiParameter { Name = "userId", In = "path" };

        // Act
        components.Parameters["UserId"] = parameter;

        // Assert
        Assert.Single(components.Parameters);
        Assert.Contains("UserId", components.Parameters.Keys);
        Assert.Equal("userId", components.Parameters["UserId"].Name);
        Assert.Equal("path", components.Parameters["UserId"].In);
    }

    [Fact]
    public void OpenApiComponents_CanAddExamples()
    {
        // Arrange
        var components = new OpenApiComponents();
        var example = new OpenApiExample { Summary = "User example" };

        // Act
        components.Examples["UserExample"] = example;

        // Assert
        Assert.Single(components.Examples);
        Assert.Contains("UserExample", components.Examples.Keys);
        Assert.Equal("User example", components.Examples["UserExample"].Summary);
    }

    [Fact]
    public void OpenApiComponents_CanAddRequestBodies()
    {
        // Arrange
        var components = new OpenApiComponents();
        var requestBody = new OpenApiRequestBody { Description = "User data" };

        // Act
        components.RequestBodies["UserRequest"] = requestBody;

        // Assert
        Assert.Single(components.RequestBodies);
        Assert.Contains("UserRequest", components.RequestBodies.Keys);
        Assert.Equal("User data", components.RequestBodies["UserRequest"].Description);
    }

    [Fact]
    public void OpenApiComponents_CanAddHeaders()
    {
        // Arrange
        var components = new OpenApiComponents();
        var header = new OpenApiHeader { Description = "Authorization header" };

        // Act
        components.Headers["Authorization"] = header;

        // Assert
        Assert.Single(components.Headers);
        Assert.Contains("Authorization", components.Headers.Keys);
        Assert.Equal("Authorization header", components.Headers["Authorization"].Description);
    }

    [Fact]
    public void OpenApiComponents_ObjectInitialization_Works()
    {
        // Act
        var components = new OpenApiComponents
        {
            Schemas = new Dictionary<string, OpenApiSchema>
            {
                ["User"] = new OpenApiSchema { Type = "object" }
            },
            Responses = new Dictionary<string, OpenApiResponse>
            {
                ["Error"] = new OpenApiResponse { Description = "Error response" }
            },
            Parameters = new Dictionary<string, OpenApiParameter>
            {
                ["Id"] = new OpenApiParameter { Name = "id" }
            },
            Examples = new Dictionary<string, OpenApiExample>
            {
                ["Example"] = new OpenApiExample { Summary = "Example" }
            },
            RequestBodies = new Dictionary<string, OpenApiRequestBody>
            {
                ["Body"] = new OpenApiRequestBody { Description = "Request body" }
            },
            Headers = new Dictionary<string, OpenApiHeader>
            {
                ["Header"] = new OpenApiHeader { Description = "Header" }
            }
        };

        // Assert
        Assert.Single(components.Schemas);
        Assert.Single(components.Responses);
        Assert.Single(components.Parameters);
        Assert.Single(components.Examples);
        Assert.Single(components.RequestBodies);
        Assert.Single(components.Headers);
    }
}
