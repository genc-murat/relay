using Relay.Core.Contracts.Handlers;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Observability
{
    /// <summary>
    /// Handler for health check requests.
    /// </summary>
    public class HealthCheckHandler : IRequestHandler<HealthCheckRequest, HealthCheckResponse>
    {
        public ValueTask<HealthCheckResponse> HandleAsync(HealthCheckRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(HealthCheckResponse.Healthy());
        }
    }
}