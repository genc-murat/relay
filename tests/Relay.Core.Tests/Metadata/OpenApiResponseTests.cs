using System.Collections.Generic;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiResponse class
/// </summary>
public class OpenApiResponseTests
{
    [Fact]
    public void OpenApiResponse_DefaultConstructor_InitializesProperties()
    {
        // Act
        var response = new OpenApiResponse();

        // Assert
        Assert.Equal(string.Empty, response.Description);
        Assert.NotNull(response.Content);
        Assert.Empty(response.Content);
        Assert.NotNull(response.Headers);
        Assert.Empty(response.Headers);
    }

    [Fact]
    public void OpenApiResponse_CanSetDescription()
    {
        // Arrange
        var response = new OpenApiResponse();

        // Act
        response.Description = "Success response";

        // Assert
        Assert.Equal("Success response", response.Description);
    }

    [Fact]
    public void OpenApiResponse_CanAddContent()
    {
        // Arrange
        var response = new OpenApiResponse();
        var mediaType = new OpenApiMediaType
        {
            Schema = new OpenApiSchema { Type = "object" }
        };

        // Act
        response.Content["application/json"] = mediaType;

        // Assert
        Assert.Single(response.Content);
        Assert.Contains("application/json", response.Content.Keys);
        Assert.NotNull(response.Content["application/json"].Schema);
        Assert.Equal("object", response.Content["application/json"].Schema.Type);
    }

    [Fact]
    public void OpenApiResponse_CanAddHeaders()
    {
        // Arrange
        var response = new OpenApiResponse();
        var header = new OpenApiHeader
        {
            Description = "Rate limit remaining",
            Schema = new OpenApiSchema { Type = "integer" }
        };

        // Act
        response.Headers["X-Rate-Limit-Remaining"] = header;

        // Assert
        Assert.Single(response.Headers);
        Assert.Contains("X-Rate-Limit-Remaining", response.Headers.Keys);
        Assert.Equal("Rate limit remaining", response.Headers["X-Rate-Limit-Remaining"].Description);
        Assert.NotNull(response.Headers["X-Rate-Limit-Remaining"].Schema);
        Assert.Equal("integer", response.Headers["X-Rate-Limit-Remaining"].Schema.Type);
    }

    [Fact]
    public void OpenApiResponse_ObjectInitialization_Works()
    {
        // Act
        var response = new OpenApiResponse
        {
            Description = "User data",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema { Type = "object" }
                }
            },
            Headers = new Dictionary<string, OpenApiHeader>
            {
                ["X-Custom-Header"] = new OpenApiHeader
                {
                    Description = "Custom header",
                    Required = false
                }
            }
        };

        // Assert
        Assert.Equal("User data", response.Description);
        Assert.Single(response.Content);
        Assert.Contains("application/json", response.Content.Keys);
        Assert.Single(response.Headers);
        Assert.Contains("X-Custom-Header", response.Headers.Keys);
        Assert.Equal("Custom header", response.Headers["X-Custom-Header"].Description);
        Assert.False(response.Headers["X-Custom-Header"].Required);
    }
}
