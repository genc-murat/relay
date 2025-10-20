using System.Collections.Generic;
#if NET6_0_OR_GREATER
#endif

namespace Relay.Core.Metadata.OpenApi
{
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