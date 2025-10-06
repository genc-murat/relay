using System;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Options;
using Relay.Core.Configuration.Resolved;

namespace Relay.Core.Configuration.Core;

/// <summary>
/// Default implementation of configuration resolver that handles attribute parameter overrides.
/// </summary>
public class ConfigurationResolver : IConfigurationResolver
{
    private readonly RelayOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationResolver"/> class.
    /// </summary>
    /// <param name="options">The relay options.</param>
    public ConfigurationResolver(IOptions<RelayOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public ResolvedHandlerConfiguration ResolveHandlerConfiguration(Type handlerType, string methodName, HandleAttribute? attribute)
    {
        if (handlerType == null) throw new ArgumentNullException(nameof(handlerType));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("Method name cannot be null or empty.", nameof(methodName));

        var key = $"{handlerType.FullName}.{methodName}";
        var globalDefaults = _options.DefaultHandlerOptions;
        var specificOverrides = _options.HandlerOverrides.TryGetValue(key, out var overrides) ? overrides : null;

        return new ResolvedHandlerConfiguration
        {
            Name = attribute?.Name,
            Priority = attribute?.Priority ?? specificOverrides?.DefaultPriority ?? globalDefaults.DefaultPriority,
            EnableCaching = specificOverrides?.EnableCaching ?? globalDefaults.EnableCaching,
            Timeout = specificOverrides?.DefaultTimeout ?? globalDefaults.DefaultTimeout,
            EnableRetry = specificOverrides?.EnableRetry ?? globalDefaults.EnableRetry,
            MaxRetryAttempts = specificOverrides?.MaxRetryAttempts ?? globalDefaults.MaxRetryAttempts
        };
    }

    /// <inheritdoc />
    public ResolvedNotificationConfiguration ResolveNotificationConfiguration(Type handlerType, string methodName, NotificationAttribute? attribute)
    {
        if (handlerType == null) throw new ArgumentNullException(nameof(handlerType));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("Method name cannot be null or empty.", nameof(methodName));

        var key = $"{handlerType.FullName}.{methodName}";
        var globalDefaults = _options.DefaultNotificationOptions;
        var specificOverrides = _options.NotificationOverrides.TryGetValue(key, out var overrides) ? overrides : null;

        return new ResolvedNotificationConfiguration
        {
            DispatchMode = attribute?.DispatchMode ?? specificOverrides?.DefaultDispatchMode ?? globalDefaults.DefaultDispatchMode,
            Priority = attribute?.Priority ?? specificOverrides?.DefaultPriority ?? globalDefaults.DefaultPriority,
            ContinueOnError = specificOverrides?.ContinueOnError ?? globalDefaults.ContinueOnError,
            Timeout = specificOverrides?.DefaultTimeout ?? globalDefaults.DefaultTimeout,
            MaxDegreeOfParallelism = specificOverrides?.MaxDegreeOfParallelism ?? globalDefaults.MaxDegreeOfParallelism
        };
    }

    /// <inheritdoc />
    public ResolvedPipelineConfiguration ResolvePipelineConfiguration(Type pipelineType, string methodName, PipelineAttribute? attribute)
    {
        if (pipelineType == null) throw new ArgumentNullException(nameof(pipelineType));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("Method name cannot be null or empty.", nameof(methodName));

        var key = $"{pipelineType.FullName}.{methodName}";
        var globalDefaults = _options.DefaultPipelineOptions;
        var specificOverrides = _options.PipelineOverrides.TryGetValue(key, out var overrides) ? overrides : null;

        return new ResolvedPipelineConfiguration
        {
            Order = attribute?.Order ?? specificOverrides?.DefaultOrder ?? globalDefaults.DefaultOrder,
            Scope = attribute?.Scope ?? specificOverrides?.DefaultScope ?? globalDefaults.DefaultScope,
            EnableCaching = specificOverrides?.EnableCaching ?? globalDefaults.EnableCaching,
            Timeout = specificOverrides?.DefaultTimeout ?? globalDefaults.DefaultTimeout
        };
    }

    /// <inheritdoc />
    public ResolvedEndpointConfiguration ResolveEndpointConfiguration(Type handlerType, string methodName, ExposeAsEndpointAttribute? attribute)
    {
        if (handlerType == null) throw new ArgumentNullException(nameof(handlerType));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("Method name cannot be null or empty.", nameof(methodName));

        var globalDefaults = _options.DefaultEndpointOptions;

        var route = attribute?.Route;
        if (string.IsNullOrWhiteSpace(route) && globalDefaults.EnableAutoRouteGeneration)
        {
            // Generate route from handler type and method name
            var typeName = handlerType.Name.EndsWith("Handler")
                ? handlerType.Name.Substring(0, handlerType.Name.Length - 7) // Remove "Handler" suffix
                : handlerType.Name;

            var prefix = !string.IsNullOrWhiteSpace(globalDefaults.DefaultRoutePrefix)
                ? $"{globalDefaults.DefaultRoutePrefix.TrimEnd('/')}/"
                : "";

            route = $"{prefix}{typeName.ToLowerInvariant()}/{methodName.ToLowerInvariant()}";
        }

        return new ResolvedEndpointConfiguration
        {
            Route = route,
            HttpMethod = attribute?.HttpMethod ?? globalDefaults.DefaultHttpMethod,
            Version = attribute?.Version ?? globalDefaults.DefaultVersion,
            EnableOpenApiGeneration = globalDefaults.EnableOpenApiGeneration,
            EnableAutoRouteGeneration = globalDefaults.EnableAutoRouteGeneration
        };
    }
}
