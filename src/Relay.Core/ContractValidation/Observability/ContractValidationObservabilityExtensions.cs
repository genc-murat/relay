using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Relay.Core.ContractValidation.Caching;

namespace Relay.Core.ContractValidation.Observability;

/// <summary>
/// Extension methods for registering contract validation observability components.
/// </summary>
public static class ContractValidationObservabilityExtensions
{
    /// <summary>
    /// Adds contract validation metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddContractValidationMetrics(this IServiceCollection services)
    {
        services.AddSingleton<ContractValidationMetrics>();
        return services;
    }

    /// <summary>
    /// Adds contract validation health checks to the health check builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check.</param>
    /// <param name="failureStatus">The health status to report on failure.</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddContractValidationHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "contract_validation",
        HealthStatus? failureStatus = null,
        string[]? tags = null)
    {
        return builder.AddCheck<ContractValidationHealthCheck>(
            name,
            failureStatus,
            tags);
    }
}
