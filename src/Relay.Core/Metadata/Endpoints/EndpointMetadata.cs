using System;
using System.Collections.Generic;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.Metadata.Endpoints
{
    /// <summary>
    /// Represents metadata for an HTTP endpoint generated from a handler.
    /// </summary>
    public class EndpointMetadata
    {
        /// <summary>
        /// Gets or sets the route template for the endpoint.
        /// </summary>
        public string Route { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTTP method for the endpoint.
        /// </summary>
        public string HttpMethod { get; set; } = "POST";

        /// <summary>
        /// Gets or sets the version of the endpoint.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the request type for the endpoint.
        /// </summary>
        public Type RequestType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the response type for the endpoint.
        /// </summary>
        public Type? ResponseType { get; set; }

        /// <summary>
        /// Gets or sets the handler type that processes this endpoint.
        /// </summary>
        public Type HandlerType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the handler method name.
        /// </summary>
        public string HandlerMethodName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON schema for the request contract.
        /// </summary>
        public JsonSchemaContract? RequestSchema { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema for the response contract.
        /// </summary>
        public JsonSchemaContract? ResponseSchema { get; set; }

        /// <summary>
        /// Gets or sets additional properties for the endpoint.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}