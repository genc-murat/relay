using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Tests.Testing;

/// <summary>
/// Utilities for measuring memory allocation during request execution
/// </summary>
public static class MemoryUtilities
{
    /// <summary>
    /// Measures memory allocation during request execution
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="relay">The relay instance</param>
    /// <param name="request">The request to measure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response and memory allocation information</returns>
    public static async Task<(TResponse Response, long AllocatedBytes)> MeasureAllocationAsync<TResponse>(
        IRelay relay,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var initialMemory = GC.GetTotalMemory(true);

        var response = await relay.SendAsync(request, cancellationToken);

        var finalMemory = GC.GetTotalMemory(false);
        var allocatedBytes = Math.Max(0, finalMemory - initialMemory);

        return (response, allocatedBytes);
    }
}