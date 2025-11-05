using System.Collections.Generic;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiOperation class
/// </summary>
public class OpenApiOperationTests
{
    [Fact]
    public void OpenApiOperation_DefaultConstructor_InitializesProperties()
    {
        // Act
        var operation = new OpenApiOperation();

        // Assert
        Assert.Null(operation.OperationId);
        Assert.Null(operation.Summary);
        Assert.Null(operation.Description);
        Assert.NotNull(operation.Tags);
        Assert.Empty(operation.Tags);
        Assert.NotNull(operation.Parameters);
        Assert.Empty(operation.Parameters);
        Assert.Null(operation.RequestBody);
        Assert.NotNull(operation.Responses);
        Assert.Empty(operation.Responses);
    }

    [Fact]
    public void OpenApiOperation_CanSetOperationId()
    {
        // Arrange
        var operation = new OpenApiOperation();

        // Act
        operation.OperationId = "getUser";

        // Assert
        Assert.Equal("getUser", operation.OperationId);
    }

    [Fact]
    public void OpenApiOperation_CanSetSummary()
    {
        // Arrange
        var operation = new OpenApiOperation();

        // Act
        operation.Summary = "Get user by ID";

        // Assert
        Assert.Equal("Get user by ID", operation.Summary);
    }

    [Fact]
    public void OpenApiOperation_CanSetDescription()
    {
        // Arrange
        var operation = new OpenApiOperation();

        // Act
        operation.Description = "Retrieves a user by their unique identifier";

        // Assert
        Assert.Equal("Retrieves a user by their unique identifier", operation.Description);
    }

    [Fact]
    public void OpenApiOperation_CanAddTags()
    {
        // Arrange
        var operation = new OpenApiOperation();

        // Act
        operation.Tags.Add("users");
        operation.Tags.Add("v1");

        // Assert
        Assert.Contains("users", operation.Tags);
        Assert.Contains("v1", operation.Tags);
        Assert.Equal(2, operation.Tags.Count);
    }

    [Fact]
    public void OpenApiOperation_CanAddParameters()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var parameter = new OpenApiParameter { Name = "userId", In = "path" };

        // Act
        operation.Parameters.Add(parameter);

        // Assert
        Assert.Single(operation.Parameters);
        Assert.Equal("userId", operation.Parameters[0].Name);
        Assert.Equal("path", operation.Parameters[0].In);
    }

    [Fact]
    public void OpenApiOperation_CanSetRequestBody()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var requestBody = new OpenApiRequestBody { Required = true };

        // Act
        operation.RequestBody = requestBody;

        // Assert
        Assert.NotNull(operation.RequestBody);
        Assert.True(operation.RequestBody.Required);
    }

    [Fact]
    public void OpenApiOperation_CanAddResponses()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var response = new OpenApiResponse { Description = "Success" };

        // Act
        operation.Responses["200"] = response;

        // Assert
        Assert.Single(operation.Responses);
        Assert.Contains("200", operation.Responses.Keys);
        Assert.Equal("Success", operation.Responses["200"].Description);
    }

    [Fact]
    public void OpenApiOperation_ObjectInitialization_Works()
    {
        // Act
        var operation = new OpenApiOperation
        {
            OperationId = "getUser",
            Summary = "Get user",
            Description = "Get user by ID",
            Tags = new List<string> { "users" },
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "id", In = "path" }
            },
            RequestBody = new OpenApiRequestBody { Required = false },
            Responses = new Dictionary<string, OpenApiResponse>
            {
                ["200"] = new OpenApiResponse { Description = "Success" }
            }
        };

        // Assert
        Assert.Equal("getUser", operation.OperationId);
        Assert.Equal("Get user", operation.Summary);
        Assert.Equal("Get user by ID", operation.Description);
        Assert.Contains("users", operation.Tags);
        Assert.Single(operation.Parameters);
        Assert.NotNull(operation.RequestBody);
        Assert.False(operation.RequestBody.Required);
        Assert.Single(operation.Responses);
        Assert.Contains("200", operation.Responses.Keys);
    }
}
