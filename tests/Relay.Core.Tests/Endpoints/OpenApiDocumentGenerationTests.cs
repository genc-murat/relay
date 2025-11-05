using System;
using System.Linq;
using System.Text.Json;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Metadata.Endpoints;
using Relay.Core.Metadata.MessageQueue;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Endpoints;

/// <summary>
/// Tests for basic OpenAPI document generation functionality
/// </summary>
public class OpenApiDocumentGenerationTests
{
    public OpenApiDocumentGenerationTests()
    {
        // Clear registry before each test
        EndpointMetadataRegistry.Clear();
    }

    [Fact]
    public void GenerateDocument_WithSingleEndpoint_CreatesValidOpenApiDocument()
    {
        // Arrange
        EndpointMetadataRegistry.Clear();
        var metadata = new EndpointMetadata
        {
            Route = "/api/users",
            HttpMethod = "POST",
            Version = "v1",
            RequestType = typeof(CreateUserRequest),
            ResponseType = typeof(UserResponse),
            HandlerType = typeof(UserHandler),
            HandlerMethodName = "CreateUser",
            RequestSchema = new JsonSchemaContract
            {
                Schema = @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""name"": { ""type"": ""string"" },
                            ""email"": { ""type"": ""string"" }
                        },
                        ""required"": [""name"", ""email""]
                    }",
                ContentType = "application/json"
            },
            ResponseSchema = new JsonSchemaContract
            {
                Schema = @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""id"": { ""type"": ""integer"" },
                            ""name"": { ""type"": ""string"" }
                        }
                    }",
                ContentType = "application/json"
            }
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        var options = new OpenApiGenerationOptions
        {
            Title = "Test API",
            Version = "1.0.0",
            Description = "Test API for OpenAPI generation"
        };

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument(options);

        // Assert
        Assert.Equal("3.0.1", document.OpenApi);
        Assert.Equal("Test API", document.Info.Title);
        Assert.Equal("1.0.0", document.Info.Version);
        Assert.Equal("Test API for OpenAPI generation", document.Info.Description);

        // Debug: Check how many endpoints are actually registered
        var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
        Assert.True(allEndpoints.Count == 1,
            $"Expected 1 endpoint but found {allEndpoints.Count}. Endpoints: {string.Join(", ", allEndpoints.Select(e => e.Route))}");

        Assert.Single(document.Paths);
        Assert.True(document.Paths.ContainsKey("/api/users"));

        var pathItem = document.Paths["/api/users"];
        Assert.NotNull(pathItem.Post);
        Assert.Null(pathItem.Get);

        var operation = pathItem.Post;
        Assert.Equal("postCreateUser", operation.OperationId);
        Assert.Contains("v1", operation.Tags);
        Assert.Contains("User", operation.Tags);

        Assert.NotNull(operation.RequestBody);
        Assert.True(operation.RequestBody.Required);
        Assert.Contains("application/json", operation.RequestBody.Content.Keys);

        Assert.Contains("200", operation.Responses.Keys);
        Assert.Contains("400", operation.Responses.Keys);
        Assert.Contains("500", operation.Responses.Keys);
    }

    [Fact]
    public void GenerateDocument_WithMultipleEndpoints_CreatesCorrectPaths()
    {
        // Arrange
        var metadata1 = new EndpointMetadata
        {
            Route = "/api/users",
            HttpMethod = "GET",
            RequestType = typeof(GetUsersRequest),
            ResponseType = typeof(UserResponse[]),
            HandlerType = typeof(UserHandler),
            HandlerMethodName = "GetUsers"
        };

        var metadata2 = new EndpointMetadata
        {
            Route = "/api/users",
            HttpMethod = "POST",
            RequestType = typeof(CreateUserRequest),
            ResponseType = typeof(UserResponse),
            HandlerType = typeof(UserHandler),
            HandlerMethodName = "CreateUser"
        };

        var metadata3 = new EndpointMetadata
        {
            Route = "/api/users/{id}",
            HttpMethod = "DELETE",
            RequestType = typeof(DeleteUserRequest),
            HandlerType = typeof(UserHandler),
            HandlerMethodName = "DeleteUser"
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata1);
        EndpointMetadataRegistry.RegisterEndpoint(metadata2);
        EndpointMetadataRegistry.RegisterEndpoint(metadata3);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Equal(2, document.Paths.Count);
        Assert.True(document.Paths.ContainsKey("/api/users"));
        Assert.True(document.Paths.ContainsKey("/api/users/{id}"));

        var usersPath = document.Paths["/api/users"];
        Assert.NotNull(usersPath.Get);
        Assert.NotNull(usersPath.Post);
        Assert.Null(usersPath.Delete);

        var userByIdPath = document.Paths["/api/users/{id}"];
        Assert.Null(userByIdPath.Get);
        Assert.Null(userByIdPath.Post);
        Assert.NotNull(userByIdPath.Delete);
    }

    // Test types for OpenAPI integration tests
    public class CreateUserRequest : IRequest<UserResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class GetUsersRequest : IRequest<UserResponse[]>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class DeleteUserRequest : IRequest
    {
        public int Id { get; set; }
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UserHandler
    {
        [Handle]
        [ExposeAsEndpoint]
        public UserResponse CreateUser(CreateUserRequest request) => new();

        [Handle]
        [ExposeAsEndpoint]
        public UserResponse[] GetUsers(GetUsersRequest request) => Array.Empty<UserResponse>();

        [Handle]
        [ExposeAsEndpoint]
        public void DeleteUser(DeleteUserRequest request) { }
    }
}
