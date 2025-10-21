using Relay.Core.AI;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Observability
{
    /// <summary>
    /// Test request for health checks.
    /// </summary>
    [AIMonitored(Level = MonitoringLevel.Basic, Tags = new[] { "health", "system" })]
    public record HealthCheckRequest : IRequest<HealthCheckResponse>;
}