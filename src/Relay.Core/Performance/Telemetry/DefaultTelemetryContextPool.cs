using Microsoft.Extensions.ObjectPool;
using Relay.Core.Telemetry;
using System;
using System.Diagnostics;

namespace Relay.Core.Performance.Telemetry;

/// <summary>
/// Default implementation of telemetry context pool using Microsoft.Extensions.ObjectPool
/// </summary>
public class DefaultTelemetryContextPool : ITelemetryContextPool
{
    private readonly ObjectPool<TelemetryContext> _pool;

    /// <summary>
    /// Initializes a new instance of the DefaultTelemetryContextPool
    /// </summary>
    /// <param name="provider">The object pool provider</param>
    public DefaultTelemetryContextPool(ObjectPoolProvider provider)
    {
        var policy = new TelemetryContextPooledObjectPolicy();
        _pool = provider.Create(policy);
    }

    /// <summary>
    /// Gets a telemetry context from the pool
    /// </summary>
    /// <returns>A pooled telemetry context</returns>
    public TelemetryContext Get()
    {
        var context = _pool.Get();

        // Ensure fresh state even if policy didn't reset properly
        context.RequestId = Guid.NewGuid().ToString();
        context.StartTime = DateTimeOffset.UtcNow;

        return context;
    }

    /// <summary>
    /// Returns a telemetry context to the pool
    /// </summary>
    /// <param name="context">The context to return</param>
    public void Return(TelemetryContext context)
    {
        if (context != null)
        {
            _pool.Return(context);
        }
    }
}