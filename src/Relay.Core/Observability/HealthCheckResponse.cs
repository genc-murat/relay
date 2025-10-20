using System;

namespace Relay.Core.Observability
{
    /// <summary>
    /// Test response for health checks.
    /// </summary>
    public record HealthCheckResponse(bool IsHealthy, DateTime Timestamp)
    {
        public static HealthCheckResponse Healthy() => new(true, DateTime.UtcNow);
    }
}