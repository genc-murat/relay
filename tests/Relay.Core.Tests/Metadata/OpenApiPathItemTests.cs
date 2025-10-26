using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiPathItem class
/// </summary>
public class OpenApiPathItemTests
{
    [Fact]
    public void OpenApiPathItem_DefaultConstructor_InitializesProperties()
    {
        // Act
        var pathItem = new OpenApiPathItem();

        // Assert
        Assert.Null(pathItem.Get);
        Assert.Null(pathItem.Post);
        Assert.Null(pathItem.Put);
        Assert.Null(pathItem.Delete);
        Assert.Null(pathItem.Patch);
        Assert.Null(pathItem.Head);
        Assert.Null(pathItem.Options);
        Assert.Null(pathItem.Trace);
    }

    [Fact]
    public void OpenApiPathItem_CanSetGetOperation()
    {
        // Arrange
        var pathItem = new OpenApiPathItem();
        var operation = new OpenApiOperation { OperationId = "getUser" };

        // Act
        pathItem.Get = operation;

        // Assert
        Assert.NotNull(pathItem.Get);
        Assert.Equal("getUser", pathItem.Get.OperationId);
    }

    [Fact]
    public void OpenApiPathItem_CanSetPostOperation()
    {
        // Arrange
        var pathItem = new OpenApiPathItem();
        var operation = new OpenApiOperation { OperationId = "createUser" };

        // Act
        pathItem.Post = operation;

        // Assert
        Assert.NotNull(pathItem.Post);
        Assert.Equal("createUser", pathItem.Post.OperationId);
    }

    [Fact]
    public void OpenApiPathItem_CanSetPutOperation()
    {
        // Arrange
        var pathItem = new OpenApiPathItem();
        var operation = new OpenApiOperation { OperationId = "updateUser" };

        // Act
        pathItem.Put = operation;

        // Assert
        Assert.NotNull(pathItem.Put);
        Assert.Equal("updateUser", pathItem.Put.OperationId);
    }

    [Fact]
    public void OpenApiPathItem_CanSetDeleteOperation()
    {
        // Arrange
        var pathItem = new OpenApiPathItem();
        var operation = new OpenApiOperation { OperationId = "deleteUser" };

        // Act
        pathItem.Delete = operation;

        // Assert
        Assert.NotNull(pathItem.Delete);
        Assert.Equal("deleteUser", pathItem.Delete.OperationId);
    }

    [Fact]
    public void OpenApiPathItem_CanSetPatchOperation()
    {
        // Arrange
        var pathItem = new OpenApiPathItem();
        var operation = new OpenApiOperation { OperationId = "patchUser" };

        // Act
        pathItem.Patch = operation;

        // Assert
        Assert.NotNull(pathItem.Patch);
        Assert.Equal("patchUser", pathItem.Patch.OperationId);
    }

    [Fact]
    public void OpenApiPathItem_CanSetHeadOperation()
    {
        // Arrange
        var pathItem = new OpenApiPathItem();
        var operation = new OpenApiOperation { OperationId = "headUser" };

        // Act
        pathItem.Head = operation;

        // Assert
        Assert.NotNull(pathItem.Head);
        Assert.Equal("headUser", pathItem.Head.OperationId);
    }

    [Fact]
    public void OpenApiPathItem_CanSetOptionsOperation()
    {
        // Arrange
        var pathItem = new OpenApiPathItem();
        var operation = new OpenApiOperation { OperationId = "optionsUser" };

        // Act
        pathItem.Options = operation;

        // Assert
        Assert.NotNull(pathItem.Options);
        Assert.Equal("optionsUser", pathItem.Options.OperationId);
    }

    [Fact]
    public void OpenApiPathItem_CanSetTraceOperation()
    {
        // Arrange
        var pathItem = new OpenApiPathItem();
        var operation = new OpenApiOperation { OperationId = "traceUser" };

        // Act
        pathItem.Trace = operation;

        // Assert
        Assert.NotNull(pathItem.Trace);
        Assert.Equal("traceUser", pathItem.Trace.OperationId);
    }

    [Fact]
    public void OpenApiPathItem_ObjectInitialization_Works()
    {
        // Act
        var pathItem = new OpenApiPathItem
        {
            Get = new OpenApiOperation { OperationId = "getUser" },
            Post = new OpenApiOperation { OperationId = "createUser" },
            Put = new OpenApiOperation { OperationId = "updateUser" },
            Delete = new OpenApiOperation { OperationId = "deleteUser" }
        };

        // Assert
        Assert.NotNull(pathItem.Get);
        Assert.Equal("getUser", pathItem.Get.OperationId);
        Assert.NotNull(pathItem.Post);
        Assert.Equal("createUser", pathItem.Post.OperationId);
        Assert.NotNull(pathItem.Put);
        Assert.Equal("updateUser", pathItem.Put.OperationId);
        Assert.NotNull(pathItem.Delete);
        Assert.Equal("deleteUser", pathItem.Delete.OperationId);
        Assert.Null(pathItem.Patch);
        Assert.Null(pathItem.Head);
        Assert.Null(pathItem.Options);
        Assert.Null(pathItem.Trace);
    }

    [Fact]
    public void OpenApiPathItem_CanSetOperationsToNull()
    {
        // Arrange
        var pathItem = new OpenApiPathItem
        {
            Get = new OpenApiOperation { OperationId = "test" },
            Post = new OpenApiOperation { OperationId = "test" }
        };

        // Act
        pathItem.Get = null;
        pathItem.Post = null;

        // Assert
        Assert.Null(pathItem.Get);
        Assert.Null(pathItem.Post);
    }
}