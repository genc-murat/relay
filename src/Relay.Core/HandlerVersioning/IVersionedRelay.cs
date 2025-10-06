using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.HandlerVersioning;

/// <summary>
/// Interface for versioned handler dispatch
/// Allows multiple versions of handlers to coexist and be selected based on version requirements
/// </summary>
public interface IVersionedRelay : IRelay
{
    /// <summary>
    /// Sends a request to a specific handler version
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">The request</param>
    /// <param name="version">Required handler version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the versioned handler</returns>
    ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request, 
        Version version, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request to the handler with the highest compatible version
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">The request</param>
    /// <param name="minVersion">Minimum compatible version</param>
    /// <param name="maxVersion">Maximum compatible version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the compatible handler</returns>
    ValueTask<TResponse> SendCompatibleAsync<TResponse>(
        IRequest<TResponse> request,
        Version? minVersion = null,
        Version? maxVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available versions for a request type
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <returns>Available versions</returns>
    IReadOnlyList<Version> GetAvailableVersions<TRequest>() where TRequest : IRequest;

    /// <summary>
    /// Gets the latest version for a request type
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <returns>Latest version, or null if no handlers found</returns>
    Version? GetLatestVersion<TRequest>() where TRequest : IRequest;
}


