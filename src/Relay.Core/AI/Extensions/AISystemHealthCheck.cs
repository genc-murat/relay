using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.Core.AI
{
    /// <summary>
    /// Health check for overall AI system.
    /// </summary>
    internal class AISystemHealthCheck
    {
        private readonly IAIOptimizationEngine _engine;
        private readonly ILogger<AISystemHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AISystemHealthCheck(
            IAIOptimizationEngine engine,
            ILogger<AISystemHealthCheck> logger,
            IOptions<AIHealthCheckOptions> options)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new AIHealthCheckOptions();
        }

        public async Task<ComponentHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ComponentHealthResult
            {
                ComponentName = "AI System",
                Data = new Dictionary<string, object>()
            };

            try
            {
                var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromMinutes(10), cancellationToken);
                var healthScore = insights.HealthScore;

                result.IsHealthy = healthScore != null && healthScore.Overall >= _options.MinSystemHealthScore;
                result.HealthScore = healthScore?.Overall ?? 0.0;
                result.Status = healthScore?.Status ?? "Unknown";
                result.Description = $"System health: {result.Status} (Score: {result.HealthScore:P1})";

                // Add system metrics
                if (healthScore != null)
                {
                    result.Data["Overall"] = healthScore.Overall;
                    result.Data["Performance"] = healthScore.Performance;
                    result.Data["Reliability"] = healthScore.Reliability;
                    result.Data["Scalability"] = healthScore.Scalability;
                    result.Data["Security"] = healthScore.Security;
                    result.Data["Maintainability"] = healthScore.Maintainability;

                    // Add critical areas as warnings
                    if (healthScore.CriticalAreas?.Count > 0)
                    {
                        foreach (var area in healthScore.CriticalAreas)
                        {
                            result.Warnings.Add($"Critical area: {area}");
                        }
                    }
                }

                // Add insight metrics
                result.Data["PerformanceGrade"] = insights.PerformanceGrade;
                result.Data["BottleneckCount"] = insights.Bottlenecks?.Count ?? 0;
                result.Data["OpportunityCount"] = insights.Opportunities?.Count ?? 0;

                // Check for severe bottlenecks
                if (insights.Bottlenecks != null && insights.Bottlenecks.Count > 5)
                {
                    result.Warnings.Add($"Multiple performance bottlenecks detected: {insights.Bottlenecks.Count}");
                }

                _logger.LogDebug("AI System health check: {Status}, Score: {Score:P1}, Warnings: {WarningCount}", 
                    result.Status, result.HealthScore, result.Warnings.Count);
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.Status = "Failed";
                result.Description = $"System health check failed: {ex.Message}";
                result.Exception = ex;
                result.Errors.Add(ex.Message);

                _logger.LogError(ex, "AI System health check failed");
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }
    }
}
