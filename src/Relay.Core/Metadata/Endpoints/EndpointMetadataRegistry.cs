using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.Metadata.Endpoints
{
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
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

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