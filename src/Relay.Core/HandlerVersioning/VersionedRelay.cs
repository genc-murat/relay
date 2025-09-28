using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        var handlerVersion = FindHandlerVersion(typeof(IRequest<TResponse>), version);
        if (handlerVersion == null)
        {
            throw new HandlerVersionNotFoundException(typeof(IRequest<TResponse>), version);
        }

        _logger.LogDebug("Dispatching {RequestType} to handler version {Version}",
            typeof(IRequest<TResponse>).Name, version);

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

        var compatibleVersion = FindCompatibleVersion(typeof(IRequest<TResponse>), minVersion, maxVersion);
        if (compatibleVersion == null)
        {
            throw new HandlerVersionNotFoundException(typeof(IRequest<TResponse>), minVersion ?? new Version(0, 0), maxVersion);
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
            // In a full implementation, this would scan for handlers with HandlerVersionAttribute
            // For now, return a default version
            return new List<HandlerVersionInfo>
            {
                new HandlerVersionInfo
                {
                    Version = new Version(1, 0, 0),
                    IsDeprecated = false,
                    HandlerType = typeof(object) // Placeholder
                }
            };
        });
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

/// <summary>
/// Information about a handler version
/// </summary>
internal sealed class HandlerVersionInfo
{
    public Version Version { get; init; } = new(1, 0);
    public bool IsDeprecated { get; init; }
    public string? DeprecationMessage { get; init; }
    public Type HandlerType { get; init; } = typeof(object);
}

/// <summary>
/// Exception thrown when a handler version is not found
/// </summary>
public sealed class HandlerVersionNotFoundException : Exception
{
    public Type RequestType { get; }
    public Version? RequestedVersion { get; }
    public Version? MinVersion { get; }
    public Version? MaxVersion { get; }

    public HandlerVersionNotFoundException(Type requestType, Version requestedVersion)
        : base("Handler version {requestedVersion} not found for request type {requestType.Name}")
    {
        RequestType = requestType;
        RequestedVersion = requestedVersion;
    }

    public HandlerVersionNotFoundException(Type requestType, Version? minVersion, Version? maxVersion)
        : base("No compatible handler version found for request type {requestType.Name} (min: {minVersion}, max: {maxVersion})")
    {
        RequestType = requestType;
        MinVersion = minVersion;
        MaxVersion = maxVersion;
    }
}
