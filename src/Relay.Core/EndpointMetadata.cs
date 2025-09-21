using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core
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

    /// <summary>
    /// Registry for endpoint metadata generated at compile time.
    /// </summary>
    public static class EndpointMetadataRegistry
    {
        private static readonly object _lock = new();
        private static readonly System.Threading.AsyncLocal<Guid?> _currentScope = new();
        private static readonly Dictionary<Guid, Dictionary<Type, List<EndpointMetadata>>> _endpointsByRequestType = new();
        private static readonly Dictionary<Guid, List<EndpointMetadata>> _allEndpoints = new();

        /// <summary>
        /// Gets all registered endpoint metadata.
        /// </summary>
        public static IReadOnlyList<EndpointMetadata> AllEndpoints
        {
            get
            {
                lock (_lock)
                {
                    var scope = _currentScope.Value;
                    if (scope.HasValue && _allEndpoints.TryGetValue(scope.Value, out var list))
                    {
                        return list.ToList().AsReadOnly();
                    }
                    return Array.Empty<EndpointMetadata>();
                }
            }
        }

        /// <summary>
        /// Registers endpoint metadata for a request type.
        /// </summary>
        /// <param name="metadata">The endpoint metadata to register.</param>
        public static void RegisterEndpoint(EndpointMetadata metadata)
        {
            lock (_lock)
            {
                var scope = EnsureScopeInitialized_NoLock();

                if (!_endpointsByRequestType[scope].TryGetValue(metadata.RequestType, out var list))
                {
                    list = new List<EndpointMetadata>();
                    _endpointsByRequestType[scope][metadata.RequestType] = list;
                }

                list.Add(metadata);
                _allEndpoints[scope].Add(metadata);
            }
        }

        /// <summary>
        /// Gets endpoint metadata for a specific request type.
        /// </summary>
        /// <param name="requestType">The request type to get endpoints for.</param>
        /// <returns>The endpoint metadata for the request type, or empty list if none found.</returns>
        public static IReadOnlyList<EndpointMetadata> GetEndpointsForRequestType(Type requestType)
        {
            lock (_lock)
            {
                var scope = _currentScope.Value;
                if (scope.HasValue && _endpointsByRequestType.TryGetValue(scope.Value, out var dict) && dict.TryGetValue(requestType, out var endpoints))
                {
                    return endpoints.ToList().AsReadOnly();
                }
                return Array.Empty<EndpointMetadata>();
            }
        }

        /// <summary>
        /// Gets endpoint metadata for a specific request type.
        /// </summary>
        /// <typeparam name="TRequest">The request type to get endpoints for.</typeparam>
        /// <returns>The endpoint metadata for the request type, or empty list if none found.</returns>
        public static IReadOnlyList<EndpointMetadata> GetEndpointsForRequestType<TRequest>()
        {
            return GetEndpointsForRequestType(typeof(TRequest));
        }

        /// <summary>
        /// Clears all registered endpoint metadata. Used for testing.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                var scope = Guid.NewGuid();
                _currentScope.Value = scope;
                _endpointsByRequestType[scope] = new Dictionary<Type, List<EndpointMetadata>>();
                _allEndpoints[scope] = new List<EndpointMetadata>();
            }
        }

        private static Guid EnsureScopeInitialized_NoLock()
        {
            if (_currentScope.Value is Guid s && _allEndpoints.ContainsKey(s))
            {
                return s;
            }
            var scope = _currentScope.Value ?? Guid.NewGuid();
            _currentScope.Value = scope;
            if (!_allEndpoints.ContainsKey(scope))
            {
                _allEndpoints[scope] = new List<EndpointMetadata>();
            }
            if (!_endpointsByRequestType.ContainsKey(scope))
            {
                _endpointsByRequestType[scope] = new Dictionary<Type, List<EndpointMetadata>>();
            }
            return scope;
        }
    }
}