using Relay.Core.Contracts.Requests;

namespace Relay.Core.Observability
{
    /// <summary>
    /// Test request for health checks.
    /// </summary>
    public record HealthCheckRequest : IRequest<HealthCheckResponse>;
}