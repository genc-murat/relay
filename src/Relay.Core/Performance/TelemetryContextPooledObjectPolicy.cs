using Microsoft.Extensions.ObjectPool;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;

namespace Relay.Core.Performance;

/// <summary>
/// Object pool policy for telemetry contexts
/// </summary>
public class TelemetryContextPooledObjectPolicy : IPooledObjectPolicy<TelemetryContext>
{
    /// <summary>
    /// Creates a new telemetry context
    /// </summary>
    /// <returns>A new telemetry context instance</returns>
    public TelemetryContext Create()
    {
        return new TelemetryContext();
    }

    /// <summary>
    /// Returns a telemetry context to the pool after resetting its state
    /// </summary>
    /// <param name="obj">The telemetry context to return</param>
    /// <returns>True if the object can be returned to the pool</returns>
    public bool Return(TelemetryContext obj)
    {
        if (obj == null)
            return false;

        // Reset the context state for reuse
        obj.RequestId = Guid.NewGuid().ToString();
        obj.CorrelationId = null;
        obj.RequestType = null!;
        obj.ResponseType = null;
        obj.HandlerName = null;
        obj.StartTime = DateTimeOffset.UtcNow;
        obj.Activity = null;

        // Clear properties dictionary but keep the instance to avoid allocations
        obj.Properties.Clear();

        return true;
    }
}