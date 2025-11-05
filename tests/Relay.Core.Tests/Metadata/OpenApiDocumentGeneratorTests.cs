using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Metadata.Endpoints;
using Relay.Core.Metadata.MessageQueue;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class OpenApiDocumentGeneratorTests
    {
        public OpenApiDocumentGeneratorTests()
        {
            // Clear registry before each test
            EndpointMetadataRegistry.Clear();
        }

        [Fact]
        public void GenerateDocument_WithDefaultOptions_CreatesValidDocument()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "GET",
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HandlerType = typeof(TestHandler),
                HandlerMethodName = "HandleAsync"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument();

            // Assert
            Assert.Equal("3.0.1", document.OpenApi);
            Assert.Equal("Relay API", document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
            Assert.Single(document.Servers);
            Assert.Single(document.Paths);
            Assert.Contains("/api/test", document.Paths.Keys);
        }

        [Fact]
        public void GenerateDocument_WithCustomOptions_UsesProvidedOptions()
        {
            // Arrange
            var options = new OpenApiGenerationOptions
            {
                Title = "Custom API",
                Description = "Custom description",
                Version = "2.0.0"
            };
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(options);

            // Assert
            Assert.Equal("Custom API", document.Info.Title);
            Assert.Equal("Custom description", document.Info.Description);
            Assert.Equal("2.0.0", document.Info.Version);
        }

        [Fact]
        public void GenerateDocument_WithSpecificEndpoints_GeneratesFromProvidedEndpoints()
        {
            // Arrange
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            var endpoints = new[] { metadata1, metadata2 };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            Assert.Equal(2, document.Paths.Count);
            Assert.Contains("/api/test1", document.Paths.Keys);
            Assert.Contains("/api/test2", document.Paths.Keys);
        }

        [Fact]
        public void GenerateDocument_WithEmptyEndpoints_CreatesEmptyDocument()
        {
            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(Enumerable.Empty<EndpointMetadata>());

            // Assert
            Assert.Empty(document.Paths);
            Assert.NotNull(document.Components);
            Assert.NotNull(document.Components.Schemas);
            Assert.Empty(document.Components.Schemas);
        }

        [Fact]
        public void GenerateDocument_WithMultipleHttpMethodsOnSameRoute_CreatesMultipleOperations()
        {
            // Arrange
            var getMetadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "GET",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            var postMetadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "POST",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            var endpoints = new[] { getMetadata, postMetadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            Assert.Single(document.Paths);
            var pathItem = document.Paths["/api/test"];
            Assert.NotNull(pathItem.Get);
            Assert.NotNull(pathItem.Post);
            Assert.Null(pathItem.Put);
        }

        [Fact]
        public void SerializeToJson_WithDefaultOptions_ProducesValidJson()
        {
            // Arrange
            var document = new OpenApiDocument
            {
                OpenApi = "3.0.1",
                Info = new OpenApiInfo { Title = "Test API", Version = "1.0.0" },
                Paths = new Dictionary<string, OpenApiPathItem>(),
                Components = new OpenApiComponents()
            };

            // Act
            var json = OpenApiDocumentGenerator.SerializeToJson(document);

            // Assert
            Assert.Contains("\"openapi\": \"3.0.1\"", json);
            Assert.Contains("\"title\": \"Test API\"", json);
            Assert.Contains("\"version\": \"1.0.0\"", json);

            // Verify it's valid JSON
            var deserialized = JsonSerializer.Deserialize<OpenApiDocument>(json);
            Assert.NotNull(deserialized);
            Assert.Equal("3.0.1", deserialized.OpenApi);
        }

        [Fact]
        public void SerializeToJson_WithCustomOptions_UsesProvidedOptions()
        {
            // Arrange
            var document = new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test API" }
            };
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = null
            };

            // Act
            var json = OpenApiDocumentGenerator.SerializeToJson(document, options);

            // Assert
            Assert.Contains("\"openapi\"", json); // JsonPropertyName overrides PropertyNamingPolicy
            Assert.Contains("\"Info\"", json);
            Assert.DoesNotContain("\n", json); // Not indented
        }

        [Fact]
        public void GenerateDocument_IncludesComponentSchemas_ForRequestAndResponseTypes()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HandlerType = typeof(TestHandler),
                RequestSchema = new JsonSchemaContract { Schema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}" },
                ResponseSchema = new JsonSchemaContract { Schema = "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"integer\"}}}" }
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            Assert.NotNull(document.Components.Schemas);
            Assert.Contains("TestRequest", document.Components.Schemas.Keys);
            Assert.Contains("TestResponse", document.Components.Schemas.Keys);
        }

        [Fact]
        public void GenerateDocument_WithVersion_IncludesVersionInTags()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                Version = "v1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            var operation = document.Paths["/api/test"].Post; // Default is POST
            Assert.NotNull(operation);
            Assert.Contains("v1", operation.Tags);
        }

        [Fact]
        public void GenerateDocument_WithRequestBody_IncludesRequestBodyForPost()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "POST",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler),
                RequestSchema = new JsonSchemaContract { Schema = "{\"type\":\"object\"}" }
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            var operation = document.Paths["/api/test"].Post;
            Assert.NotNull(operation);
            Assert.NotNull(operation.RequestBody);
            Assert.True(operation.RequestBody.Required);
        }

        [Fact]
        public void GenerateDocument_WithResponse_IncludesSuccessResponse()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HandlerType = typeof(TestHandler),
                ResponseSchema = new JsonSchemaContract { Schema = "{\"type\":\"object\"}" }
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            var operation = document.Paths["/api/test"].Post;
            Assert.NotNull(operation);
            Assert.NotNull(operation.Responses);
            Assert.Contains("200", operation.Responses.Keys);
            var response = operation.Responses["200"];
            Assert.Equal("Success", response.Description);
            Assert.NotNull(response.Content);
        }

        [Fact]
        public void GenerateDocument_WithoutResponse_IncludesNoContentResponse()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
                // No ResponseType or ResponseSchema
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            var operation = document.Paths["/api/test"].Post;
            Assert.NotNull(operation);
            Assert.Contains("204", operation.Responses.Keys);
            var response = operation.Responses["204"];
            Assert.Equal("No Content", response.Description);
        }

        [Fact]
        public void GenerateDocument_IncludesErrorResponses()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            var operation = document.Paths["/api/test"].Post;
            Assert.NotNull(operation);
            Assert.Contains("400", operation.Responses.Keys);
            Assert.Contains("500", operation.Responses.Keys);
            Assert.Equal("Bad Request", operation.Responses["400"].Description);
            Assert.Equal("Internal Server Error", operation.Responses["500"].Description);
        }

        [Fact]
        public void GenerateDocument_GeneratesOperationId_Correctly()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "POST",
                RequestType = typeof(CreateUserRequest),
                HandlerType = typeof(TestHandler)
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            var operation = document.Paths["/api/test"].Post;
            Assert.NotNull(operation);
            Assert.Equal("postCreateUser", operation.OperationId);
        }

        [Fact]
        public void GenerateDocument_GeneratesSummary_Correctly()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "GET",
                RequestType = typeof(GetUserRequest),
                HandlerType = typeof(TestHandler)
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            var operation = document.Paths["/api/test"].Get;
            Assert.NotNull(operation);
            Assert.Equal("GET GetUserRequest", operation.Summary);
        }

        [Fact]
        public void GenerateDocument_GeneratesDescription_Correctly()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(UserHandler),
                HandlerMethodName = "HandleAsync"
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            var operation = document.Paths["/api/test"].Post;
            Assert.NotNull(operation);
            Assert.Equal("Handles TestRequest via UserHandler.HandleAsync", operation.Description);
        }

        [Fact]
        public void GenerateDocument_GeneratesTags_Correctly()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                Version = "v1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(UserHandler)
            };

            var endpoints = new[] { metadata };

            // Act
            var document = OpenApiDocumentGenerator.GenerateDocument(endpoints);

            // Assert
            var operation = document.Paths["/api/test"].Post;
            Assert.NotNull(operation);
            Assert.Contains("v1", operation.Tags);
            Assert.Contains("User", operation.Tags); // Handler name without "Handler" suffix
        }

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class CreateUserRequest : IRequest<TestResponse> { }
        private class GetUserRequest : IRequest<TestResponse> { }
        private class TestHandler { }
        private class UserHandler { }
    }
}
