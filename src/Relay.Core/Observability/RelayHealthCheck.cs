using Relay.Core.Contracts.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Observability;

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