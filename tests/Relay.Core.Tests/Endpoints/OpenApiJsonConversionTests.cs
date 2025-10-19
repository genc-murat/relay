using System;
using System.Linq;
using System.Text.Json;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Xunit;
using static Relay.Core.Tests.Endpoints.OpenApiDocumentGenerationTests;

namespace Relay.Core.Tests.Endpoints;

/// <summary>
/// Tests for JSON object to OpenAPI schema conversion functionality
/// </summary>
public class OpenApiJsonConversionTests
{
    public OpenApiJsonConversionTests()
    {
        // Clear registry before each test
        EndpointMetadataRegistry.Clear();
    }

    [Fact]
    public void GenerateDocument_WithComplexJsonSchema_IncludesAllSchemaProperties()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/complex-schema",
            HttpMethod = "POST",
            RequestType = typeof(ComplexSchemaRequest),
            HandlerType = typeof(OpenApiJsonConversionTests),
            HandlerMethodName = "HandleComplexSchema",
            RequestSchema = new JsonSchemaContract
            {
                Schema = @"{
                        ""type"": ""object"",
                        ""title"": ""ComplexSchemaRequest"",
                        ""description"": ""A complex request with various schema properties"",
                        ""properties"": {
                            ""name"": {
                                ""type"": ""string"",
                                ""format"": ""email"",
                                ""description"": ""User email address""
                            },
                            ""status"": {
                                ""type"": ""string"",
                                ""enum"": [""active"", ""inactive"", ""pending""],
                                ""description"": ""User status""
                            },
                            ""count"": {
                                ""type"": ""integer"",
                                ""format"": ""int32"",
                                ""description"": ""Item count""
                            }
                        }
                    }"
            }
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Single(document.Components.Schemas);
        var schema = document.Components.Schemas["ComplexSchemaRequest"];

        Assert.Equal("object", schema.Type);
        Assert.Equal("ComplexSchemaRequest", schema.Title);
        Assert.Equal("A complex request with various schema properties", schema.Description);

        Assert.Contains("name", schema.Properties.Keys);
        Assert.Contains("status", schema.Properties.Keys);
        Assert.Contains("count", schema.Properties.Keys);

        var nameProperty = schema.Properties["name"];
        Assert.Equal("string", nameProperty.Type);
        Assert.Equal("email", nameProperty.Format);
        Assert.Equal("User email address", nameProperty.Description);

        var statusProperty = schema.Properties["status"];
        Assert.Equal("string", statusProperty.Type);
        Assert.Equal(3, statusProperty.Enum.Count);
        Assert.Contains("active", statusProperty.Enum);
        Assert.Contains("inactive", statusProperty.Enum);
        Assert.Contains("pending", statusProperty.Enum);
        Assert.Equal("User status", statusProperty.Description);

        var countProperty = schema.Properties["count"];
        Assert.Equal("integer", countProperty.Type);
        Assert.Equal("int32", countProperty.Format);
        Assert.Equal("Item count", countProperty.Description);
    }

    [Fact]
    public void ConvertJsonObjectToOpenApiSchema_WithSimpleObject_CreatesCorrectSchema()
    {
        // Arrange
        EndpointMetadataRegistry.Clear();
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
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
                        ""title"": ""SimpleObject"",
                        ""description"": ""A simple object schema"",
                        ""properties"": {
                            ""name"": { ""type"": ""string"" },
                            ""age"": { ""type"": ""integer"", ""format"": ""int32"" }
                        },
                        ""required"": [""name""]
                    }",
                ContentType = "application/json"
            }
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Single(document.Components.Schemas);
        var schema = document.Components.Schemas["CreateUserRequest"];

        Assert.Equal("object", schema.Type);
        Assert.Equal("SimpleObject", schema.Title);
        Assert.Equal("A simple object schema", schema.Description);

        Assert.Contains("name", schema.Properties.Keys);
        Assert.Contains("age", schema.Properties.Keys);

        var nameProperty = schema.Properties["name"];
        Assert.Equal("string", nameProperty.Type);

        var ageProperty = schema.Properties["age"];
        Assert.Equal("integer", ageProperty.Type);
        Assert.Equal("int32", ageProperty.Format);

        Assert.Contains("name", schema.Required);
        Assert.DoesNotContain("age", schema.Required);
    }

    [Fact]
    public void ConvertJsonObjectToOpenApiSchema_WithArrayProperty_CreatesCorrectSchema()
    {
        // Arrange
        EndpointMetadataRegistry.Clear();
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
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
                            ""tags"": {
                                ""type"": ""array"",
                                ""items"": { ""type"": ""string"" }
                            }
                        }
                    }",
                ContentType = "application/json"
            }
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Single(document.Components.Schemas);
        var schema = document.Components.Schemas.First().Value;

        Assert.Contains("tags", schema.Properties.Keys);
        var tagsProperty = schema.Properties["tags"];

        Assert.Equal("array", tagsProperty.Type);
        Assert.NotNull(tagsProperty.Items);
        Assert.Equal("string", tagsProperty.Items.Type);
    }

    [Fact]
    public void ConvertJsonObjectToOpenApiSchema_WithEnumProperty_CreatesCorrectSchema()
    {
        // Arrange
        EndpointMetadataRegistry.Clear();
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
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
                            ""status"": {
                                ""type"": ""string"",
                                ""enum"": [""active"", ""inactive"", ""pending""],
                                ""description"": ""User status""
                            }
                        }
                    }",
                ContentType = "application/json"
            }
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Single(document.Components.Schemas);
        var schema = document.Components.Schemas.First().Value;

        Assert.Contains("status", schema.Properties.Keys);
        var statusProperty = schema.Properties["status"];

        Assert.Equal("string", statusProperty.Type);
        Assert.Equal("User status", statusProperty.Description);
        Assert.NotNull(statusProperty.Enum);
        Assert.Equal(3, statusProperty.Enum.Count);
        Assert.Contains("active", statusProperty.Enum);
        Assert.Contains("inactive", statusProperty.Enum);
        Assert.Contains("pending", statusProperty.Enum);
    }

    [Fact]
    public void ConvertJsonObjectToOpenApiSchema_WithNestedObject_CreatesCorrectSchema()
    {
        // Arrange
        EndpointMetadataRegistry.Clear();
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
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
                            ""user"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""name"": { ""type"": ""string"" },
                                    ""email"": { ""type"": ""string"", ""format"": ""email"" }
                                },
                                ""required"": [""name""]
                            }
                        }
                    }",
                ContentType = "application/json"
            }
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Single(document.Components.Schemas);
        var schema = document.Components.Schemas.First().Value;

        Assert.Contains("user", schema.Properties.Keys);
        var userProperty = schema.Properties["user"];

        Assert.Equal("object", userProperty.Type);
        Assert.Contains("name", userProperty.Properties.Keys);
        Assert.Contains("email", userProperty.Properties.Keys);

        var nameProperty = userProperty.Properties["name"];
        Assert.Equal("string", nameProperty.Type);

        var emailProperty = userProperty.Properties["email"];
        Assert.Equal("string", emailProperty.Type);
        Assert.Equal("email", emailProperty.Format);

        Assert.Contains("name", userProperty.Required);
        Assert.DoesNotContain("email", userProperty.Required);
    }

    [Fact]
    public void ConvertJsonObjectToOpenApiSchema_WithEmptyObject_CreatesBasicSchema()
    {
        // Arrange
        EndpointMetadataRegistry.Clear();
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "POST",
            Version = "v1",
            RequestType = typeof(CreateUserRequest),
            ResponseType = typeof(UserResponse),
            HandlerType = typeof(UserHandler),
            HandlerMethodName = "CreateUser",
            RequestSchema = new JsonSchemaContract
            {
                Schema = @"{
                        ""type"": ""object""
                    }",
                ContentType = "application/json"
            }
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Single(document.Components.Schemas);
        var schema = document.Components.Schemas.First().Value;

        Assert.Equal("object", schema.Type);
        Assert.Empty(schema.Properties);
        Assert.Empty(schema.Required);
    }

    [Fact]
    public void ConvertJsonObjectToOpenApiSchema_WithComplexNestedStructure_CreatesCorrectSchema()
    {
        // Arrange
        EndpointMetadataRegistry.Clear();
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
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
                        ""title"": ""ComplexRequest"",
                        ""description"": ""A complex request with nested structures"",
                        ""properties"": {
                            ""user"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""name"": { ""type"": ""string"" },
                                    ""preferences"": {
                                        ""type"": ""object"",
                                        ""properties"": {
                                            ""theme"": { ""type"": ""string"", ""enum"": [""light"", ""dark""] },
                                            ""notifications"": { ""type"": ""boolean"" }
                                        }
                                    }
                                },
                                ""required"": [""name""]
                            },
                            ""items"": {
                                ""type"": ""array"",
                                ""items"": {
                                    ""type"": ""object"",
                                    ""properties"": {
                                        ""id"": { ""type"": ""integer"" },
                                        ""name"": { ""type"": ""string"" }
                                    },
                                    ""required"": [""id""]
                                }
                            }
                        },
                        ""required"": [""user""]
                    }",
                ContentType = "application/json"
            }
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument();

        // Assert
        Assert.Single(document.Components.Schemas);
        var schema = document.Components.Schemas["CreateUserRequest"];

        Assert.Equal("object", schema.Type);
        Assert.Equal("ComplexRequest", schema.Title);
        Assert.Equal("A complex request with nested structures", schema.Description);

        // Check user property
        Assert.Contains("user", schema.Properties.Keys);
        var userProperty = schema.Properties["user"];
        Assert.Equal("object", userProperty.Type);

        // Check nested preferences
        Assert.Contains("preferences", userProperty.Properties.Keys);
        var preferencesProperty = userProperty.Properties["preferences"];
        Assert.Equal("object", preferencesProperty.Type);

        // Check theme enum
        Assert.Contains("theme", preferencesProperty.Properties.Keys);
        var themeProperty = preferencesProperty.Properties["theme"];
        Assert.Equal("string", themeProperty.Type);
        Assert.Contains("light", themeProperty.Enum);
        Assert.Contains("dark", themeProperty.Enum);

        // Check items array
        Assert.Contains("items", schema.Properties.Keys);
        var itemsProperty = schema.Properties["items"];
        Assert.Equal("array", itemsProperty.Type);
        Assert.NotNull(itemsProperty.Items);

        // Check array item properties
        var itemSchema = itemsProperty.Items;
        Assert.Contains("id", itemSchema.Properties.Keys);
        Assert.Contains("name", itemSchema.Properties.Keys);
        Assert.Contains("id", itemSchema.Required);

        // Check required fields
        Assert.Contains("user", schema.Required);
    }

    // Test types for OpenAPI JSON conversion tests
    public class CreateUserRequest : IRequest<UserResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
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

    public class GetUsersRequest : IRequest<UserResponse[]>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ComplexSchemaRequest : IRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}