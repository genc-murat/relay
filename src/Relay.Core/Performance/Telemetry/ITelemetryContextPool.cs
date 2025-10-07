using Relay.Core.Telemetry;

namespace Relay.Core.Performance.Telemetry;

/// <summary>
/// Interface for pooling telemetry contexts to reduce allocations
/// </summary>
public interface ITelemetryContextPool
{
    /// <summary>
    /// Gets a telemetry context from the pool
    /// </summary>
    /// <returns>A pooled telemetry context</returns>
    TelemetryContext Get();

    /// <summary>
    /// Returns a telemetry context to the pool
    /// </summary>
    /// <param name="context">The context to return</param>
    void Return(TelemetryContext context);
}