using System;

namespace Relay.Core
{
    /// <summary>
    /// Attribute to mark handlers for automatic HTTP endpoint generation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ExposeAsEndpointAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the route template for the endpoint.
        /// </summary>
        public string? Route { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method for the endpoint.
        /// </summary>
        public string HttpMethod { get; set; } = "POST";

        /// <summary>
        /// Gets or sets the version of the endpoint for API versioning.
        /// </summary>
        public string? Version { get; set; }
    }
}