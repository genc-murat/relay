using System;
using System.Linq;
using System.Text.Json;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests
{
    public class OpenApiIntegrationTests
    {
        public OpenApiIntegrationTests()
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

        [Fact]
        public void SerializeToJson_WithValidDocument_ProducesValidJson()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "GET",
                RequestType = typeof(string),
                ResponseType = typeof(string),
                HandlerType = typeof(OpenApiIntegrationTests),
                HandlerMethodName = "TestMethod"
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);
            var document = OpenApiDocumentGenerator.GenerateDocument();

            // Act
            var json = OpenApiDocumentGenerator.SerializeToJson(document);

            // Assert
            Assert.NotEmpty(json);

            // Verify it's valid JSON
            var parsedDocument = JsonDocument.Parse(json);
            Assert.NotNull(parsedDocument);

            // Verify basic structure  
            var root = parsedDocument.RootElement;
            // OpenAPI property might be serialized as "openApi" due to camelCase policy
            var hasOpenApiProperty = root.TryGetProperty("openapi", out var openApiProperty) ||
                                   root.TryGetProperty("openApi", out openApiProperty);
            Assert.True(hasOpenApiProperty, "Missing 'openapi' or 'openApi' property in JSON");
            Assert.Equal("3.0.1", openApiProperty.GetString());

            Assert.True(root.TryGetProperty("info", out var infoProperty));
            Assert.True(infoProperty.TryGetProperty("title", out var titleProperty));
            Assert.Equal("Relay API", titleProperty.GetString());

            Assert.True(root.TryGetProperty("paths", out var pathsProperty));
            Assert.True(pathsProperty.TryGetProperty("/api/test", out _));
        }

        [Fact]
        public void GenerateDocument_WithCustomOptions_UsesCustomValues()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "GET",
                RequestType = typeof(string),
                HandlerType = typeof(OpenApiIntegrationTests),
                HandlerMethodName = "TestMethod"
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            var options = new OpenApiGenerationOptions
            {
                Title = "Custom API",
                Description = "Custom description",
                Version = "2.0.0",
                Contact = new OpenApiContact
                {
                    Name = "Test Contact",
                    Email = "test@example.com",
                    Url = "https://example.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = "https://opensource.org/licenses/MIT"
                }
            };

            options.Servers.Clear();
            options.Servers.Add(new OpenApiServer
            {
                Url = "https://api.example.com",
                Description = "Production server"
            });

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(options);

            // Assert
            Assert.Equal("Custom API", document.Info.Title);
            Assert.Equal("Custom description", document.Info.Description);
            Assert.Equal("2.0.0", document.Info.Version);

            Assert.NotNull(document.Info.Contact);
            Assert.Equal("Test Contact", document.Info.Contact.Name);
            Assert.Equal("test@example.com", document.Info.Contact.Email);
            Assert.Equal("https://example.com", document.Info.Contact.Url);

            Assert.NotNull(document.Info.License);
            Assert.Equal("MIT", document.Info.License.Name);
            Assert.Equal("https://opensource.org/licenses/MIT", document.Info.License.Url);

            Assert.Single(document.Servers);
            Assert.Equal("https://api.example.com", document.Servers[0].Url);
            Assert.Equal("Production server", document.Servers[0].Description);
        }

        [Fact]
        public void GenerateDocument_WithComplexSchema_CreatesComponentSchemas()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/complex",
                HttpMethod = "POST",
                RequestType = typeof(ComplexRequest),
                ResponseType = typeof(ComplexResponse),
                HandlerType = typeof(OpenApiIntegrationTests),
                HandlerMethodName = "HandleComplex",
                RequestSchema = new JsonSchemaContract
                {
                    Schema = @"{
                        ""type"": ""object"",
                        ""title"": ""ComplexRequest"",
                        ""properties"": {
                            ""user"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""name"": { ""type"": ""string"" },
                                    ""age"": { ""type"": ""integer"" }
                                }
                            },
                            ""tags"": {
                                ""type"": ""array"",
                                ""items"": { ""type"": ""string"" }
                            }
                        }
                    }"
                },
                ResponseSchema = new JsonSchemaContract
                {
                    Schema = @"{
                        ""type"": ""object"",
                        ""title"": ""ComplexResponse"",
                        ""properties"": {
                            ""id"": { ""type"": ""integer"" },
                            ""success"": { ""type"": ""boolean"" }
                        }
                    }"
                }
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument();

            // Assert
            Assert.Equal(2, document.Components.Schemas.Count);
            Assert.True(document.Components.Schemas.ContainsKey("ComplexRequest"));
            Assert.True(document.Components.Schemas.ContainsKey("ComplexResponse"));

            var requestSchema = document.Components.Schemas["ComplexRequest"];
            Assert.Equal("object", requestSchema.Type);
            Assert.Equal("ComplexRequest", requestSchema.Title);
            Assert.Contains("user", requestSchema.Properties.Keys);
            Assert.Contains("tags", requestSchema.Properties.Keys);

            var responseSchema = document.Components.Schemas["ComplexResponse"];
            Assert.Equal("object", responseSchema.Type);
            Assert.Equal("ComplexResponse", responseSchema.Title);
            Assert.Contains("id", responseSchema.Properties.Keys);
            Assert.Contains("success", responseSchema.Properties.Keys);
        }

        [Fact]
        public void GenerateDocument_WithAllHttpMethods_CoversAllMethodBranches()
        {
            // Arrange
            var endpoints = new[]
            {
                new EndpointMetadata { Route = "/api/test", HttpMethod = "GET", RequestType = typeof(string), HandlerType = typeof(OpenApiIntegrationTests), HandlerMethodName = "GetMethod" },
                new EndpointMetadata { Route = "/api/test", HttpMethod = "POST", RequestType = typeof(string), HandlerType = typeof(OpenApiIntegrationTests), HandlerMethodName = "PostMethod" },
                new EndpointMetadata { Route = "/api/test", HttpMethod = "PUT", RequestType = typeof(string), HandlerType = typeof(OpenApiIntegrationTests), HandlerMethodName = "PutMethod" },
                new EndpointMetadata { Route = "/api/test", HttpMethod = "DELETE", RequestType = typeof(string), HandlerType = typeof(OpenApiIntegrationTests), HandlerMethodName = "DeleteMethod" },
                new EndpointMetadata { Route = "/api/test", HttpMethod = "PATCH", RequestType = typeof(string), HandlerType = typeof(OpenApiIntegrationTests), HandlerMethodName = "PatchMethod" },
                new EndpointMetadata { Route = "/api/test", HttpMethod = "HEAD", RequestType = typeof(string), HandlerType = typeof(OpenApiIntegrationTests), HandlerMethodName = "HeadMethod" },
                new EndpointMetadata { Route = "/api/test", HttpMethod = "OPTIONS", RequestType = typeof(string), HandlerType = typeof(OpenApiIntegrationTests), HandlerMethodName = "OptionsMethod" },
                new EndpointMetadata { Route = "/api/test", HttpMethod = "TRACE", RequestType = typeof(string), HandlerType = typeof(OpenApiIntegrationTests), HandlerMethodName = "TraceMethod" }
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

        [Fact]
        public void GenerateOperationId_WithCommandAndQuerySuffixes_RemovesSuffixes()
        {
            // Arrange
            var commandMetadata = new EndpointMetadata
            {
                Route = "/api/command",
                HttpMethod = "POST",
                RequestType = typeof(OpenApiTestCommand),
                HandlerType = typeof(OpenApiIntegrationTests),
                HandlerMethodName = "HandleCommand"
            };

            var queryMetadata = new EndpointMetadata
            {
                Route = "/api/query",
                HttpMethod = "GET",
                RequestType = typeof(OpenApiTestQuery),
                HandlerType = typeof(OpenApiIntegrationTests),
                HandlerMethodName = "HandleQuery"
            };

            EndpointMetadataRegistry.RegisterEndpoint(commandMetadata);
            EndpointMetadataRegistry.RegisterEndpoint(queryMetadata);

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument();

            // Assert
            Assert.Equal(2, document.Paths.Count);
            var commandPath = document.Paths["/api/command"];
            var queryPath = document.Paths["/api/query"];

            Assert.Equal("postOpenApiTest", commandPath.Post.OperationId);
            Assert.Equal("getOpenApiTest", queryPath.Get.OperationId);
        }

        [Fact]
        public void ConvertJsonSchemaToOpenApiSchema_WithInvalidJson_ReturnsDefaultSchema()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act
            var schema = typeof(OpenApiDocumentGenerator)
                .GetMethod("ConvertJsonSchemaToOpenApiSchema", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { invalidJson }) as OpenApiSchema;

            // Assert
            Assert.NotNull(schema);
            Assert.Equal("object", schema.Type);
        }

        [Fact]
        public void GenerateDocument_WithNullOptions_UsesDefaultOptions()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "GET",
                RequestType = typeof(string),
                HandlerType = typeof(OpenApiIntegrationTests),
                HandlerMethodName = "TestMethod"
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument((OpenApiGenerationOptions)null!);

            // Assert
            Assert.Equal("3.0.1", document.OpenApi);
            Assert.Equal("Relay API", document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
            Assert.Single(document.Servers);
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
                HandlerType = typeof(OpenApiIntegrationTests),
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

    public class ComplexRequest : IRequest<ComplexResponse>
    {
        public UserInfo User { get; set; } = new();
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class ComplexResponse
    {
        public int Id { get; set; }
        public bool Success { get; set; }
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

    public class OpenApiTestCommand : IRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class OpenApiTestQuery : IRequest<string>
    {
        public int Id { get; set; }
    }

    public class ComplexSchemaRequest : IRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}