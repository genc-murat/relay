using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
#if NET6_0_OR_GREATER
using System.Text.Json.Nodes;
#endif

namespace Relay.Core
{
    /// <summary>
    /// Generates OpenAPI documents from endpoint metadata.
    /// </summary>
    public static class OpenApiDocumentGenerator
    {
        /// <summary>
        /// Generates an OpenAPI document from all registered endpoint metadata.
        /// </summary>
        /// <param name="options">Options for generating the OpenAPI document.</param>
        /// <returns>The generated OpenAPI document.</returns>
        public static OpenApiDocument GenerateDocument(OpenApiGenerationOptions? options = null)
        {
            options ??= new OpenApiGenerationOptions();

            var endpoints = EndpointMetadataRegistry.AllEndpoints;
            return GenerateDocument(endpoints, options);
        }

        /// <summary>
        /// Generates an OpenAPI document from the specified endpoint metadata.
        /// </summary>
        /// <param name="endpoints">The endpoint metadata to generate the document from.</param>
        /// <param name="options">Options for generating the OpenAPI document.</param>
        /// <returns>The generated OpenAPI document.</returns>
        public static OpenApiDocument GenerateDocument(IEnumerable<EndpointMetadata> endpoints, OpenApiGenerationOptions? options = null)
        {
            options ??= new OpenApiGenerationOptions();

            var document = new OpenApiDocument
            {
                OpenApi = "3.0.1",
                Info = new OpenApiInfo
                {
                    Title = options.Title,
                    Description = options.Description,
                    Version = options.Version,
                    Contact = options.Contact,
                    License = options.License
                },
                Servers = options.Servers.ToList(),
                Paths = new Dictionary<string, OpenApiPathItem>(),
                Components = new OpenApiComponents()
            };

            var endpointList = endpoints.ToList();

            // Generate paths from endpoints
            GeneratePaths(document, endpointList, options);

            // Generate component schemas
            GenerateComponentSchemas(document, endpointList, options);

            return document;
        }

        /// <summary>
        /// Serializes an OpenAPI document to JSON.
        /// </summary>
        /// <param name="document">The OpenAPI document to serialize.</param>
        /// <param name="options">JSON serialization options.</param>
        /// <returns>The JSON representation of the OpenAPI document.</returns>
        public static string SerializeToJson(OpenApiDocument document, JsonSerializerOptions? options = null)
        {
            options ??= new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(document, options);
        }

        private static void GeneratePaths(OpenApiDocument document, List<EndpointMetadata> endpoints, OpenApiGenerationOptions options)
        {
            var pathGroups = endpoints.GroupBy(e => e.Route);

            foreach (var pathGroup in pathGroups)
            {
                var route = pathGroup.Key;
                var pathItem = new OpenApiPathItem();

                foreach (var endpoint in pathGroup)
                {
                    var operation = GenerateOperation(endpoint, options);

                    switch (endpoint.HttpMethod.ToUpperInvariant())
                    {
                        case "GET":
                            pathItem.Get = operation;
                            break;
                        case "POST":
                            pathItem.Post = operation;
                            break;
                        case "PUT":
                            pathItem.Put = operation;
                            break;
                        case "DELETE":
                            pathItem.Delete = operation;
                            break;
                        case "PATCH":
                            pathItem.Patch = operation;
                            break;
                        case "HEAD":
                            pathItem.Head = operation;
                            break;
                        case "OPTIONS":
                            pathItem.Options = operation;
                            break;
                        case "TRACE":
                            pathItem.Trace = operation;
                            break;
                    }
                }

                document.Paths[route] = pathItem;
            }
        }

        private static OpenApiOperation GenerateOperation(EndpointMetadata endpoint, OpenApiGenerationOptions options)
        {
            var operation = new OpenApiOperation
            {
                OperationId = GenerateOperationId(endpoint),
                Summary = GenerateSummary(endpoint),
                Description = GenerateDescription(endpoint),
                Tags = GenerateTags(endpoint, options)
            };

            // Add request body for methods that typically have one
            if (ShouldHaveRequestBody(endpoint.HttpMethod) && endpoint.RequestSchema != null)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Description = $"The {endpoint.RequestType.Name} request",
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        [endpoint.RequestSchema.ContentType] = new OpenApiMediaType
                        {
                            Schema = ConvertJsonSchemaToOpenApiSchema(endpoint.RequestSchema.Schema)
                        }
                    }
                };
            }

            // Add responses
            operation.Responses = GenerateResponses(endpoint);

            return operation;
        }

        private static string GenerateOperationId(EndpointMetadata endpoint)
        {
            var method = endpoint.HttpMethod.ToLowerInvariant();
            var typeName = endpoint.RequestType.Name;

            // Remove common suffixes
            if (typeName.EndsWith("Request"))
                typeName = typeName.Substring(0, typeName.Length - 7);
            else if (typeName.EndsWith("Command"))
                typeName = typeName.Substring(0, typeName.Length - 7);
            else if (typeName.EndsWith("Query"))
                typeName = typeName.Substring(0, typeName.Length - 5);

            return $"{method}{typeName}";
        }

        private static string GenerateSummary(EndpointMetadata endpoint)
        {
            var typeName = endpoint.RequestType.Name;
            var method = endpoint.HttpMethod.ToUpperInvariant();

            return $"{method} {typeName}";
        }

        private static string GenerateDescription(EndpointMetadata endpoint)
        {
            var handlerName = endpoint.HandlerType.Name;
            var methodName = endpoint.HandlerMethodName;

            return $"Handles {endpoint.RequestType.Name} via {handlerName}.{methodName}";
        }

        private static List<string> GenerateTags(EndpointMetadata endpoint, OpenApiGenerationOptions options)
        {
            var tags = new List<string>();

            // Add version as tag if specified
            if (!string.IsNullOrWhiteSpace(endpoint.Version))
            {
                tags.Add(endpoint.Version);
            }

            // Add handler type as tag
            var handlerTypeName = endpoint.HandlerType.Name;
            if (handlerTypeName.EndsWith("Handler"))
            {
                handlerTypeName = handlerTypeName.Substring(0, handlerTypeName.Length - 7);
            }
            tags.Add(handlerTypeName);

            return tags;
        }

        private static bool ShouldHaveRequestBody(string httpMethod)
        {
            return httpMethod.ToUpperInvariant() switch
            {
                "POST" or "PUT" or "PATCH" => true,
                _ => false
            };
        }

        private static Dictionary<string, OpenApiResponse> GenerateResponses(EndpointMetadata endpoint)
        {
            var responses = new Dictionary<string, OpenApiResponse>();

            // Success response
            if (endpoint.ResponseType != null && endpoint.ResponseSchema != null)
            {
                responses["200"] = new OpenApiResponse
                {
                    Description = "Success",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        [endpoint.ResponseSchema.ContentType] = new OpenApiMediaType
                        {
                            Schema = ConvertJsonSchemaToOpenApiSchema(endpoint.ResponseSchema.Schema)
                        }
                    }
                };
            }
            else
            {
                responses["204"] = new OpenApiResponse
                {
                    Description = "No Content"
                };
            }

            // Error responses
            responses["400"] = new OpenApiResponse
            {
                Description = "Bad Request"
            };

            responses["500"] = new OpenApiResponse
            {
                Description = "Internal Server Error"
            };

            return responses;
        }

        private static void GenerateComponentSchemas(OpenApiDocument document, List<EndpointMetadata> endpoints, OpenApiGenerationOptions options)
        {
            var schemas = new Dictionary<string, OpenApiSchema>();

            foreach (var endpoint in endpoints)
            {
                // Add request schema
                if (endpoint.RequestSchema != null)
                {
                    var requestSchemaName = endpoint.RequestType.Name;
                    if (!schemas.ContainsKey(requestSchemaName))
                    {
                        schemas[requestSchemaName] = ConvertJsonSchemaToOpenApiSchema(endpoint.RequestSchema.Schema);
                    }
                }

                // Add response schema
                if (endpoint.ResponseType != null && endpoint.ResponseSchema != null)
                {
                    var responseSchemaName = endpoint.ResponseType.Name;
                    if (!schemas.ContainsKey(responseSchemaName))
                    {
                        schemas[responseSchemaName] = ConvertJsonSchemaToOpenApiSchema(endpoint.ResponseSchema.Schema);
                    }
                }
            }

            document.Components.Schemas = schemas;
        }

        private static OpenApiSchema ConvertJsonSchemaToOpenApiSchema(string jsonSchema)
        {
            try
            {
                var jsonNode = JsonNode.Parse(jsonSchema);
                if (jsonNode is JsonObject jsonObject)
                {
                    return ConvertJsonObjectToOpenApiSchema(jsonObject);
                }
            }
            catch (JsonException)
            {
                // If parsing fails, return a basic object schema
            }

            return new OpenApiSchema { Type = "object" };
        }

#if NET6_0_OR_GREATER
        private static OpenApiSchema ConvertJsonObjectToOpenApiSchema(JsonObject jsonObject)
        {
            var schema = new OpenApiSchema();

            if (jsonObject.TryGetPropertyValue("type", out var typeNode))
            {
                schema.Type = typeNode?.ToString();
            }

            if (jsonObject.TryGetPropertyValue("format", out var formatNode))
            {
                schema.Format = formatNode?.ToString();
            }

            if (jsonObject.TryGetPropertyValue("title", out var titleNode))
            {
                schema.Title = titleNode?.ToString();
            }

            if (jsonObject.TryGetPropertyValue("description", out var descriptionNode))
            {
                schema.Description = descriptionNode?.ToString();
            }

            if (jsonObject.TryGetPropertyValue("properties", out var propertiesNode) && propertiesNode is JsonObject propertiesObject)
            {
                foreach (var property in propertiesObject)
                {
                    if (property.Value is JsonObject propertyObject)
                    {
                        schema.Properties[property.Key] = ConvertJsonObjectToOpenApiSchema(propertyObject);
                    }
                }
            }

            if (jsonObject.TryGetPropertyValue("required", out var requiredNode) && requiredNode is JsonArray requiredArray)
            {
                schema.Required = requiredArray.Select(item => item?.ToString() ?? string.Empty).ToList();
            }

            if (jsonObject.TryGetPropertyValue("items", out var itemsNode) && itemsNode is JsonObject itemsObject)
            {
                schema.Items = ConvertJsonObjectToOpenApiSchema(itemsObject);
            }

            if (jsonObject.TryGetPropertyValue("enum", out var enumNode) && enumNode is JsonArray enumArray)
            {
                schema.Enum = enumArray.Select(item => (object)(item?.ToString() ?? string.Empty)).ToList();
            }

            return schema;
        }
#endif
    }

    /// <summary>
    /// Options for generating OpenAPI documents.
    /// </summary>
    public class OpenApiGenerationOptions
    {
        /// <summary>
        /// Gets or sets the title of the API.
        /// </summary>
        public string Title { get; set; } = "Relay API";

        /// <summary>
        /// Gets or sets the description of the API.
        /// </summary>
        public string? Description { get; set; } = "API generated from Relay handlers";

        /// <summary>
        /// Gets or sets the version of the API.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the contact information for the API.
        /// </summary>
        public OpenApiContact? Contact { get; set; }

        /// <summary>
        /// Gets or sets the license information for the API.
        /// </summary>
        public OpenApiLicense? License { get; set; }

        /// <summary>
        /// Gets or sets the servers for the API.
        /// </summary>
        public List<OpenApiServer> Servers { get; set; } = new()
        {
            new OpenApiServer { Url = "https://localhost:5001", Description = "Development server" }
        };

        /// <summary>
        /// Gets or sets whether to include version information in tags.
        /// </summary>
        public bool IncludeVersionInTags { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include handler information in descriptions.
        /// </summary>
        public bool IncludeHandlerInfo { get; set; } = true;
    }
}