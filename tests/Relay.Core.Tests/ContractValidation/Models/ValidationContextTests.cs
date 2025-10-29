using Relay.Core.ContractValidation.Models;
using Relay.Core.Metadata.MessageQueue;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Models;

public class ValidationContextTests
{
    private class TestRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    private class TestResponse
    {
        public int Id { get; set; }
    }

    [Fact]
    public void ForRequest_ShouldCreateRequestContext()
    {
        // Arrange
        var objectType = typeof(TestRequest);
        var objectInstance = new TestRequest { Name = "Test" };
        var schema = new JsonSchemaContract { Schema = "{}" };

        // Act
        var context = ValidationContext.ForRequest(objectType, objectInstance, schema);

        // Assert
        Assert.Equal(objectType, context.ObjectType);
        Assert.Equal(objectInstance, context.ObjectInstance);
        Assert.Equal(schema, context.Schema);
        Assert.True(context.IsRequest);
        Assert.Null(context.Metadata);
        Assert.Null(context.HandlerName);
    }

    [Fact]
    public void ForResponse_ShouldCreateResponseContext()
    {
        // Arrange
        var objectType = typeof(TestResponse);
        var objectInstance = new TestResponse { Id = 123 };
        var schema = new JsonSchemaContract { Schema = "{}" };

        // Act
        var context = ValidationContext.ForResponse(objectType, objectInstance, schema);

        // Assert
        Assert.Equal(objectType, context.ObjectType);
        Assert.Equal(objectInstance, context.ObjectInstance);
        Assert.Equal(schema, context.Schema);
        Assert.False(context.IsRequest);
    }

    [Fact]
    public void ValidationContext_WithMetadata_ShouldSetCorrectly()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        var context = new ValidationContext
        {
            ObjectType = typeof(TestRequest),
            Metadata = metadata
        };

        // Assert
        Assert.NotNull(context.Metadata);
        Assert.Equal(2, context.Metadata.Count);
        Assert.Equal("value1", context.Metadata["key1"]);
        Assert.Equal(42, context.Metadata["key2"]);
    }

    [Fact]
    public void ValidationContext_WithHandlerName_ShouldSetCorrectly()
    {
        // Arrange
        var handlerName = "TestHandler";

        // Act
        var context = new ValidationContext
        {
            ObjectType = typeof(TestRequest),
            HandlerName = handlerName
        };

        // Assert
        Assert.Equal(handlerName, context.HandlerName);
    }

    [Fact]
    public void ValidationContext_WithNullSchema_ShouldAllowNull()
    {
        // Act
        var context = ValidationContext.ForRequest(typeof(TestRequest), null, null);

        // Assert
        Assert.Null(context.Schema);
        Assert.Null(context.ObjectInstance);
    }

    [Fact]
    public void ValidationContext_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange
        var objectType = typeof(TestRequest);
        var objectInstance = new TestRequest { Name = "Test" };
        var schema = new JsonSchemaContract { Schema = "{}" };
        var metadata = new Dictionary<string, object> { ["key"] = "value" };
        var handlerName = "TestHandler";

        // Act
        var context = new ValidationContext
        {
            ObjectType = objectType,
            ObjectInstance = objectInstance,
            Schema = schema,
            IsRequest = true,
            Metadata = metadata,
            HandlerName = handlerName
        };

        // Assert
        Assert.Equal(objectType, context.ObjectType);
        Assert.Equal(objectInstance, context.ObjectInstance);
        Assert.Equal(schema, context.Schema);
        Assert.True(context.IsRequest);
        Assert.Equal(metadata, context.Metadata);
        Assert.Equal(handlerName, context.HandlerName);
    }
}
