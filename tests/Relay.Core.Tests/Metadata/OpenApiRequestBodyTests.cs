using System.Collections.Generic;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiRequestBody class
/// </summary>
public class OpenApiRequestBodyTests
{
    [Fact]
    public void OpenApiRequestBody_DefaultConstructor_InitializesProperties()
    {
        // Act
        var requestBody = new OpenApiRequestBody();

        // Assert
        Assert.Null(requestBody.Description);
        Assert.True(requestBody.Required);
        Assert.NotNull(requestBody.Content);
        Assert.Empty(requestBody.Content);
    }

    [Fact]
    public void OpenApiRequestBody_CanSetDescription()
    {
        // Arrange
        var requestBody = new OpenApiRequestBody();

        // Act
        requestBody.Description = "User data";

        // Assert
        Assert.Equal("User data", requestBody.Description);
    }

    [Fact]
    public void OpenApiRequestBody_CanSetRequired()
    {
        // Arrange
        var requestBody = new OpenApiRequestBody();

        // Act
        requestBody.Required = false;

        // Assert
        Assert.False(requestBody.Required);
    }

    [Fact]
    public void OpenApiRequestBody_CanAddContent()
    {
        // Arrange
        var requestBody = new OpenApiRequestBody();
        var mediaType = new OpenApiMediaType
        {
            Schema = new OpenApiSchema { Type = "object" }
        };

        // Act
        requestBody.Content["application/json"] = mediaType;

        // Assert
        Assert.Single(requestBody.Content);
        Assert.Contains("application/json", requestBody.Content.Keys);
        Assert.NotNull(requestBody.Content["application/json"].Schema);
        Assert.Equal("object", requestBody.Content["application/json"].Schema.Type);
    }

    [Fact]
    public void OpenApiRequestBody_ObjectInitialization_Works()
    {
        // Act
        var requestBody = new OpenApiRequestBody
        {
            Description = "User creation data",
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema { Type = "object" }
                }
            }
        };

        // Assert
        Assert.Equal("User creation data", requestBody.Description);
        Assert.True(requestBody.Required);
        Assert.Single(requestBody.Content);
        Assert.Contains("application/json", requestBody.Content.Keys);
    }

    [Fact]
    public void OpenApiRequestBody_CanSetDescriptionToNull()
    {
        // Arrange
        var requestBody = new OpenApiRequestBody
        {
            Description = "Test"
        };

        // Act
        requestBody.Description = null;

        // Assert
        Assert.Null(requestBody.Description);
    }
}
