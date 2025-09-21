using System;
using System.Collections.Generic;

namespace Relay.Core
{
    /// <summary>
    /// Represents an OpenAPI document generated from endpoint metadata.
    /// </summary>
    public class OpenApiDocument
    {
        /// <summary>
        /// Gets or sets the OpenAPI specification version.
        /// </summary>
        public string OpenApi { get; set; } = "3.0.1";

        /// <summary>
        /// Gets or sets the API information.
        /// </summary>
        public OpenApiInfo Info { get; set; } = new();

        /// <summary>
        /// Gets or sets the servers for the API.
        /// </summary>
        public List<OpenApiServer> Servers { get; set; } = new();

        /// <summary>
        /// Gets or sets the paths for the API endpoints.
        /// </summary>
        public Dictionary<string, OpenApiPathItem> Paths { get; set; } = new();

        /// <summary>
        /// Gets or sets the components (schemas, responses, etc.).
        /// </summary>
        public OpenApiComponents Components { get; set; } = new();

        /// <summary>
        /// Gets or sets additional properties for the document.
        /// </summary>
        public Dictionary<string, object> Extensions { get; set; } = new();
    }

    /// <summary>
    /// Represents API information in an OpenAPI document.
    /// </summary>
    public class OpenApiInfo
    {
        /// <summary>
        /// Gets or sets the title of the API.
        /// </summary>
        public string Title { get; set; } = "Relay API";

        /// <summary>
        /// Gets or sets the description of the API.
        /// </summary>
        public string? Description { get; set; }

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
    }

    /// <summary>
    /// Represents contact information in an OpenAPI document.
    /// </summary>
    public class OpenApiContact
    {
        /// <summary>
        /// Gets or sets the name of the contact.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the URL for the contact.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the email address for the contact.
        /// </summary>
        public string? Email { get; set; }
    }

    /// <summary>
    /// Represents license information in an OpenAPI document.
    /// </summary>
    public class OpenApiLicense
    {
        /// <summary>
        /// Gets or sets the name of the license.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL for the license.
        /// </summary>
        public string? Url { get; set; }
    }

    /// <summary>
    /// Represents a server in an OpenAPI document.
    /// </summary>
    public class OpenApiServer
    {
        /// <summary>
        /// Gets or sets the URL of the server.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the server.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the variables for the server URL.
        /// </summary>
        public Dictionary<string, OpenApiServerVariable> Variables { get; set; } = new();
    }

    /// <summary>
    /// Represents a server variable in an OpenAPI document.
    /// </summary>
    public class OpenApiServerVariable
    {
        /// <summary>
        /// Gets or sets the default value for the variable.
        /// </summary>
        public string Default { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the variable.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the enumerated values for the variable.
        /// </summary>
        public List<string> Enum { get; set; } = new();
    }

    /// <summary>
    /// Represents a path item in an OpenAPI document.
    /// </summary>
    public class OpenApiPathItem
    {
        /// <summary>
        /// Gets or sets the GET operation.
        /// </summary>
        public OpenApiOperation? Get { get; set; }

        /// <summary>
        /// Gets or sets the POST operation.
        /// </summary>
        public OpenApiOperation? Post { get; set; }

        /// <summary>
        /// Gets or sets the PUT operation.
        /// </summary>
        public OpenApiOperation? Put { get; set; }

        /// <summary>
        /// Gets or sets the DELETE operation.
        /// </summary>
        public OpenApiOperation? Delete { get; set; }

        /// <summary>
        /// Gets or sets the PATCH operation.
        /// </summary>
        public OpenApiOperation? Patch { get; set; }

        /// <summary>
        /// Gets or sets the HEAD operation.
        /// </summary>
        public OpenApiOperation? Head { get; set; }

        /// <summary>
        /// Gets or sets the OPTIONS operation.
        /// </summary>
        public OpenApiOperation? Options { get; set; }

        /// <summary>
        /// Gets or sets the TRACE operation.
        /// </summary>
        public OpenApiOperation? Trace { get; set; }
    }

    /// <summary>
    /// Represents an operation in an OpenAPI document.
    /// </summary>
    public class OpenApiOperation
    {
        /// <summary>
        /// Gets or sets the operation ID.
        /// </summary>
        public string? OperationId { get; set; }

        /// <summary>
        /// Gets or sets the summary of the operation.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Gets or sets the description of the operation.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the tags for the operation.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets the parameters for the operation.
        /// </summary>
        public List<OpenApiParameter> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the request body for the operation.
        /// </summary>
        public OpenApiRequestBody? RequestBody { get; set; }

        /// <summary>
        /// Gets or sets the responses for the operation.
        /// </summary>
        public Dictionary<string, OpenApiResponse> Responses { get; set; } = new();
    }

    /// <summary>
    /// Represents a parameter in an OpenAPI document.
    /// </summary>
    public class OpenApiParameter
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the location of the parameter (query, header, path, cookie).
        /// </summary>
        public string In { get; set; } = "query";

        /// <summary>
        /// Gets or sets whether the parameter is required.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the description of the parameter.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the schema for the parameter.
        /// </summary>
        public OpenApiSchema? Schema { get; set; }
    }

    /// <summary>
    /// Represents a request body in an OpenAPI document.
    /// </summary>
    public class OpenApiRequestBody
    {
        /// <summary>
        /// Gets or sets the description of the request body.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether the request body is required.
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// Gets or sets the content types for the request body.
        /// </summary>
        public Dictionary<string, OpenApiMediaType> Content { get; set; } = new();
    }

    /// <summary>
    /// Represents a response in an OpenAPI document.
    /// </summary>
    public class OpenApiResponse
    {
        /// <summary>
        /// Gets or sets the description of the response.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content types for the response.
        /// </summary>
        public Dictionary<string, OpenApiMediaType> Content { get; set; } = new();

        /// <summary>
        /// Gets or sets the headers for the response.
        /// </summary>
        public Dictionary<string, OpenApiHeader> Headers { get; set; } = new();
    }

    /// <summary>
    /// Represents a media type in an OpenAPI document.
    /// </summary>
    public class OpenApiMediaType
    {
        /// <summary>
        /// Gets or sets the schema for the media type.
        /// </summary>
        public OpenApiSchema? Schema { get; set; }

        /// <summary>
        /// Gets or sets examples for the media type.
        /// </summary>
        public Dictionary<string, OpenApiExample> Examples { get; set; } = new();
    }

    /// <summary>
    /// Represents a header in an OpenAPI document.
    /// </summary>
    public class OpenApiHeader
    {
        /// <summary>
        /// Gets or sets the description of the header.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether the header is required.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the schema for the header.
        /// </summary>
        public OpenApiSchema? Schema { get; set; }
    }

    /// <summary>
    /// Represents an example in an OpenAPI document.
    /// </summary>
    public class OpenApiExample
    {
        /// <summary>
        /// Gets or sets the summary of the example.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Gets or sets the description of the example.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the value of the example.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the external value reference for the example.
        /// </summary>
        public string? ExternalValue { get; set; }
    }

    /// <summary>
    /// Represents a schema in an OpenAPI document.
    /// </summary>
    public class OpenApiSchema
    {
        /// <summary>
        /// Gets or sets the type of the schema.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the format of the schema.
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets the title of the schema.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the schema.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the properties of the schema (for object types).
        /// </summary>
        public Dictionary<string, OpenApiSchema> Properties { get; set; } = new();

        /// <summary>
        /// Gets or sets the required properties of the schema.
        /// </summary>
        public List<string> Required { get; set; } = new();

        /// <summary>
        /// Gets or sets the items schema (for array types).
        /// </summary>
        public OpenApiSchema? Items { get; set; }

        /// <summary>
        /// Gets or sets the enumerated values for the schema.
        /// </summary>
        public List<object> Enum { get; set; } = new();

        /// <summary>
        /// Gets or sets the reference to another schema.
        /// </summary>
        public string? Ref { get; set; }
    }

    /// <summary>
    /// Represents the components section of an OpenAPI document.
    /// </summary>
    public class OpenApiComponents
    {
        /// <summary>
        /// Gets or sets the schemas in the components.
        /// </summary>
        public Dictionary<string, OpenApiSchema> Schemas { get; set; } = new();

        /// <summary>
        /// Gets or sets the responses in the components.
        /// </summary>
        public Dictionary<string, OpenApiResponse> Responses { get; set; } = new();

        /// <summary>
        /// Gets or sets the parameters in the components.
        /// </summary>
        public Dictionary<string, OpenApiParameter> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the examples in the components.
        /// </summary>
        public Dictionary<string, OpenApiExample> Examples { get; set; } = new();

        /// <summary>
        /// Gets or sets the request bodies in the components.
        /// </summary>
        public Dictionary<string, OpenApiRequestBody> RequestBodies { get; set; } = new();

        /// <summary>
        /// Gets or sets the headers in the components.
        /// </summary>
        public Dictionary<string, OpenApiHeader> Headers { get; set; } = new();
    }
}