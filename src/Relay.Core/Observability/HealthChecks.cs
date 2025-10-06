using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Observability
{
    /// <summary>
    /// Health check for Relay framework components.
    /// </summary>
    public class RelayHealthCheck
    {
        private readonly IRelay _relay;

        public RelayHealthCheck(IRelay relay)
        {
            _relay = relay ?? throw new ArgumentNullException(nameof(relay));
        }

        public async Task<RelayHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Test basic relay functionality
                var testRequest = new HealthCheckRequest();
                await _relay.SendAsync(testRequest, cancellationToken);

                return RelayHealthCheckResult.Healthy("Relay framework is operational");
            }
            catch (Exception ex)
            {
                return RelayHealthCheckResult.Unhealthy("Relay framework is not operational", ex);
            }
        }
    }

    /// <summary>
    /// Health check result for Relay operations.
    /// </summary>
    public class RelayHealthCheckResult
    {
        public bool IsHealthy { get; }
        public string Description { get; }
        public Exception? Exception { get; }

        private RelayHealthCheckResult(bool isHealthy, string description, Exception? exception = null)
        {
            IsHealthy = isHealthy;
            Description = description;
            Exception = exception;
        }

        public static RelayHealthCheckResult Healthy(string description) => new(true, description);
        public static RelayHealthCheckResult Unhealthy(string description, Exception? exception = null) => new(false, description, exception);
    }

    /// <summary>
    /// Test request for health checks.
    /// </summary>
    public record HealthCheckRequest : IRequest<HealthCheckResponse>;

    /// <summary>
    /// Test response for health checks.
    /// </summary>
    public record HealthCheckResponse(bool IsHealthy, DateTime Timestamp)
    {
        public static HealthCheckResponse Healthy() => new(true, DateTime.UtcNow);
    }

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