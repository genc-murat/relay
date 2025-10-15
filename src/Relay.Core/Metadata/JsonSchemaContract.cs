using System.Collections.Generic;

namespace Relay.Core
{
    /// <summary>
    /// Represents a JSON schema contract for request/response types.
    /// </summary>
    public class JsonSchemaContract
    {
        /// <summary>
        /// Gets or sets the JSON schema definition.
        /// </summary>
        public string Schema { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content type for the contract.
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Gets or sets the schema format version.
        /// </summary>
        public string SchemaVersion { get; set; } = "http://json-schema.org/draft-07/schema#";

        /// <summary>
        /// Gets or sets additional schema properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}