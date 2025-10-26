using System.Collections.Generic;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiMediaType class
/// </summary>
public class OpenApiMediaTypeTests
{
    [Fact]
    public void OpenApiMediaType_DefaultConstructor_InitializesProperties()
    {
        // Act
        var mediaType = new OpenApiMediaType();

        // Assert
        Assert.Null(mediaType.Schema);
        Assert.NotNull(mediaType.Examples);
        Assert.Empty(mediaType.Examples);
    }

    [Fact]
    public void OpenApiMediaType_CanSetSchema()
    {
        // Arrange
        var mediaType = new OpenApiMediaType();
        var schema = new OpenApiSchema { Type = "string" };

        // Act
        mediaType.Schema = schema;

        // Assert
        Assert.NotNull(mediaType.Schema);
        Assert.Equal("string", mediaType.Schema.Type);
    }

    [Fact]
    public void OpenApiMediaType_CanAddExamples()
    {
        // Arrange
        var mediaType = new OpenApiMediaType();
        var example = new OpenApiExample
        {
            Summary = "Sample user",
            Value = new { name = "John Doe", age = 30 }
        };

        // Act
        mediaType.Examples["userExample"] = example;

        // Assert
        Assert.Single(mediaType.Examples);
        Assert.Contains("userExample", mediaType.Examples.Keys);
        Assert.Equal("Sample user", mediaType.Examples["userExample"].Summary);
        Assert.NotNull(mediaType.Examples["userExample"].Value);
    }

    [Fact]
    public void OpenApiMediaType_ObjectInitialization_Works()
    {
        // Act
        var mediaType = new OpenApiMediaType
        {
            Schema = new OpenApiSchema { Type = "object" },
            Examples = new Dictionary<string, OpenApiExample>
            {
                ["example1"] = new OpenApiExample { Summary = "Example 1" }
            }
        };

        // Assert
        Assert.NotNull(mediaType.Schema);
        Assert.Equal("object", mediaType.Schema.Type);
        Assert.Single(mediaType.Examples);
        Assert.Contains("example1", mediaType.Examples.Keys);
        Assert.Equal("Example 1", mediaType.Examples["example1"].Summary);
    }

    [Fact]
    public void OpenApiMediaType_CanSetSchemaToNull()
    {
        // Arrange
        var mediaType = new OpenApiMediaType
        {
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        mediaType.Schema = null;

        // Assert
        Assert.Null(mediaType.Schema);
    }
}