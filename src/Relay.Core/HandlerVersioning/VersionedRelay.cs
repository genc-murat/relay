using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.HandlerVersioning;

/// <summary>
/// Default implementation of IVersionedRelay
/// Provides handler versioning support with backward compatibility
/// </summary>
public sealed class VersionedRelay : IVersionedRelay
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRelay _baseRelay;
    private readonly ILogger<VersionedRelay> _logger;
    private readonly ConcurrentDictionary<Type, List<HandlerVersionInfo>> _versionCache = new();

    public VersionedRelay(
        IServiceProvider serviceProvider,
        IRelay baseRelay,
        ILogger<VersionedRelay> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _baseRelay = baseRelay ?? throw new ArgumentNullException(nameof(baseRelay));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Delegate to base relay for non-versioned requests
        return _baseRelay.SendAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        // Delegate to base relay for non-versioned requests
        return _baseRelay.SendAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Delegate to base relay for non-versioned requests
        return _baseRelay.StreamAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        // Delegate to base relay for non-versioned requests
        return _baseRelay.PublishAsync(notification, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        Version version,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (version == null) throw new ArgumentNullException(nameof(version));

        var requestType = request.GetType();
        var handlerVersion = FindHandlerVersion(requestType, version);
        if (handlerVersion == null)
        {
            throw new HandlerVersionNotFoundException(requestType, version);
        }

        _logger.LogDebug("Dispatching {RequestType} to handler version {Version}",
            requestType.Name, version);

        // For now, delegate to base relay
        // In a full implementation, this would dispatch to the specific versioned handler
        return _baseRelay.SendAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<TResponse> SendCompatibleAsync<TResponse>(
        IRequest<TResponse> request,
        Version? minVersion = null,
        Version? maxVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var compatibleVersion = FindCompatibleVersion(requestType, minVersion, maxVersion);
        if (compatibleVersion == null)
        {
            throw new HandlerVersionNotFoundException(requestType, minVersion ?? new Version(0, 0), maxVersion);
        }

        return SendAsync(request, compatibleVersion, cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyList<Version> GetAvailableVersions<TRequest>() where TRequest : IRequest
    {
        var versions = GetVersionInfo(typeof(TRequest));
        return versions.Select(v => v.Version).OrderByDescending(v => v).ToArray();
    }

    /// <inheritdoc />
    public Version? GetLatestVersion<TRequest>() where TRequest : IRequest
    {
        var versions = GetAvailableVersions<TRequest>();
        return versions.FirstOrDefault();
    }

    private List<HandlerVersionInfo> GetVersionInfo(Type requestType)
    {
        return _versionCache.GetOrAdd(requestType, _ =>
        {
            var versionInfos = new List<HandlerVersionInfo>();

            try
            {
                // Scan for all registered handlers for this request type
                var handlers = FindHandlersForRequest(requestType);

                foreach (var handlerType in handlers)
                {
                    // Look for HandlerVersionAttribute on the HandleAsync method
                    var handleMethod = FindHandleAsyncMethod(handlerType, requestType);
                    if (handleMethod == null)
                    {
                        _logger.LogWarning("Handler {HandlerType} does not have HandleAsync method for {RequestType}",
                            handlerType.Name, requestType.Name);
                        continue;
                    }

                    var versionAttr = handleMethod.GetCustomAttribute<HandlerVersionAttribute>();
                    if (versionAttr != null)
                    {
                        if (Version.TryParse(versionAttr.Version, out var version))
                        {
                            versionInfos.Add(new HandlerVersionInfo
                            {
                                Version = version,
                                IsDeprecated = false, // Could be extended with DeprecatedAttribute
                                HandlerType = handlerType
                            });

                            _logger.LogDebug("Found handler version {Version} for {RequestType} in {HandlerType}",
                                version, requestType.Name, handlerType.Name);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid version format '{Version}' on {HandlerType}.{MethodName}",
                                versionAttr.Version, handlerType.Name, handleMethod.Name);
                        }
                    }
                    else
                    {
                        // Handler without version attribute - assign default version 1.0
                        versionInfos.Add(new HandlerVersionInfo
                        {
                            Version = new Version(1, 0),
                            IsDeprecated = false,
                            HandlerType = handlerType
                        });

                        _logger.LogDebug("Handler {HandlerType} has no version attribute, using default version 1.0",
                            handlerType.Name);
                    }
                }

                // If no handlers found, return empty list
                if (versionInfos.Count == 0)
                {
                    _logger.LogWarning("No handlers found for request type {RequestType}", requestType.Name);
                }

                return versionInfos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for handler versions for {RequestType}", requestType.Name);
                return new List<HandlerVersionInfo>();
            }
        });
    }

    private List<Type> FindHandlersForRequest(Type requestType)
    {
        var handlers = new List<Type>();

        try
        {
            // Determine the response type
            Type? responseType = null;
            var requestInterfaces = requestType.GetInterfaces();

            // Check if it's IRequest<TResponse>
            var genericRequestInterface = requestInterfaces.FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            if (genericRequestInterface != null)
            {
                responseType = genericRequestInterface.GetGenericArguments()[0];
            }

            // Look for registered handlers in the service provider
            Type handlerInterfaceType;
            if (responseType != null)
            {
                // IRequestHandler<TRequest, TResponse>
                handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            }
            else if (requestInterfaces.Any(i => i == typeof(IRequest)))
            {
                // IRequestHandler<TRequest>
                handlerInterfaceType = typeof(IRequestHandler<>).MakeGenericType(requestType);
            }
            else
            {
                _logger.LogWarning("Request type {RequestType} does not implement IRequest or IRequest<TResponse>",
                    requestType.Name);
                return handlers;
            }

            // Try to get all registered services of this handler type
            var handlerServices = _serviceProvider.GetServices(handlerInterfaceType);

            foreach (var handler in handlerServices)
            {
                if (handler != null)
                {
                    var handlerType = handler.GetType();

                    // Unwrap if it's a generated proxy/wrapper
                    while (handlerType.Name.Contains("Proxy") || handlerType.Name.Contains("Generated"))
                    {
                        handlerType = handlerType.BaseType ?? handlerType;
                        if (handlerType == typeof(object)) break;
                    }

                    if (!handlers.Contains(handlerType))
                    {
                        handlers.Add(handlerType);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding handlers for request type {RequestType}", requestType.Name);
        }

        return handlers;
    }

    private MethodInfo? FindHandleAsyncMethod(Type handlerType, Type requestType)
    {
        try
        {
            // Look for HandleAsync method
            var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                if (method.Name == "HandleAsync" || method.Name.EndsWith(".HandleAsync"))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length >= 1 &&
                        (parameters[0].ParameterType == requestType ||
                         parameters[0].ParameterType.IsAssignableFrom(requestType)))
                    {
                        return method;
                    }
                }
            }

            // Also check interface implementations explicitly
            var interfaces = handlerType.GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType)
                {
                    var genericDef = iface.GetGenericTypeDefinition();
                    if (genericDef == typeof(IRequestHandler<,>) || genericDef == typeof(IRequestHandler<>))
                    {
                        var map = handlerType.GetInterfaceMap(iface);
                        var handleAsyncMethod = map.TargetMethods.FirstOrDefault(m => m.Name.Contains("HandleAsync"));
                        if (handleAsyncMethod != null)
                        {
                            return handleAsyncMethod;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding HandleAsync method on {HandlerType}", handlerType.Name);
        }

        return null;
    }

    /// <summary>
    /// Clears the version cache. Useful for testing or when handlers are dynamically registered.
    /// </summary>
    internal void ClearVersionCache()
    {
        _versionCache.Clear();
        _logger.LogDebug("Version cache cleared");
    }

    /// <summary>
    /// Gets the count of cached version entries
    /// </summary>
    internal int GetCachedVersionCount()
    {
        return _versionCache.Count;
    }

    private HandlerVersionInfo? FindHandlerVersion(Type requestType, Version version)
    {
        var versions = GetVersionInfo(requestType);
        return versions.FirstOrDefault(v => v.Version == version);
    }

    private Version? FindCompatibleVersion(Type requestType, Version? minVersion, Version? maxVersion)
    {
        var versions = GetVersionInfo(requestType);
        var compatibleVersions = versions.Where(v =>
        {
            if (minVersion != null && v.Version < minVersion) return false;
            if (maxVersion != null && v.Version > maxVersion) return false;
            return true;
        });

        return compatibleVersions.OrderByDescending(v => v.Version).FirstOrDefault()?.Version;
    }
}
