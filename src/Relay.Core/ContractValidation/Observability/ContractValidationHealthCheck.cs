using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Relay.Core.ContractValidation.Caching;

namespace Relay.Core.ContractValidation.Observability;

/// <summary>
/// Health check for the contract validation system.
/// </summary>
public sealed class ContractValidationHealthCheck : IHealthCheck
{
    private readonly ISchemaCache? _schemaCache;
    private readonly ContractValidationMetrics? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractValidationHealthCheck"/> class.
    /// </summary>
    /// <param name="schemaCache">Optional schema cache to check.</param>
    /// <param name="metrics">Optional metrics to include in health check.</param>
    public ContractValidationHealthCheck(
        ISchemaCache? schemaCache = null,
        ContractValidationMetrics? metrics = null)
    {
        _schemaCache = schemaCache;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();

            // Check schema cache health
            if (_schemaCache != null)
            {
                var cacheMetrics = _schemaCache.GetMetrics();
                data["cache_size"] = cacheMetrics.CurrentSize;
                data["cache_max_size"] = cacheMetrics.MaxSize;
                data["cache_hit_rate"] = cacheMetrics.HitRate;
                data["cache_total_requests"] = cacheMetrics.TotalRequests;
                data["cache_evictions"] = cacheMetrics.TotalEvictions;

                // Warn if cache is nearly full
                if (cacheMetrics.CurrentSize >= cacheMetrics.MaxSize * 0.9)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "Schema cache is nearly full",
                        data: data));
                }

                // Warn if hit rate is low
                if (cacheMetrics.TotalRequests > 100 && cacheMetrics.HitRate < 0.5)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "Schema cache hit rate is low",
                        data: data));
                }
            }

            // Check metrics health
            if (_metrics != null)
            {
                var errorCounts = _metrics.GetErrorCountsByType();
                data["total_error_types"] = errorCounts.Count;

                // Add top error types
                var topErrors = new List<string>();
                foreach (var kvp in errorCounts)
                {
                    if (topErrors.Count < 5)
                    {
                        topErrors.Add($"{kvp.Key}:{kvp.Value}");
                    }
                }
                if (topErrors.Count > 0)
                {
                    data["top_errors"] = string.Join(", ", topErrors);
                }
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "Contract validation system is healthy",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Contract validation system health check failed",
                exception: ex));
        }
    }
}
