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
    /// Health check for AI Optimization Engine.
    /// </summary>
    public class AIOptimizationHealthCheck
    {
        private readonly IAIOptimizationEngine _engine;
        private readonly ILogger<AIOptimizationHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AIOptimizationHealthCheck(
            IAIOptimizationEngine engine,
            ILogger<AIOptimizationHealthCheck> logger,
            IOptions<AIHealthCheckOptions> options)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new AIHealthCheckOptions();
        }

        public virtual async Task<ComponentHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ComponentHealthResult
            {
                ComponentName = "AI Optimization Engine",
                Data = new Dictionary<string, object>()
            };

            try
            {
                // Check if engine is accessible
                var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromMinutes(5), cancellationToken);
                
                result.IsHealthy = true;
                result.Status = "Operational";
                result.HealthScore = insights.HealthScore?.Overall ?? 0.0;
                result.Description = $"Engine is operational with health score: {result.HealthScore:P1}";

                // Add insights data
                result.Data["PerformanceGrade"] = insights.PerformanceGrade;
                result.Data["HealthScore"] = insights.HealthScore?.Overall ?? 0.0;
                result.Data["BottleneckCount"] = insights.Bottlenecks?.Count ?? 0;
                result.Data["OpportunityCount"] = insights.Opportunities?.Count ?? 0;

                // Check health score threshold
                if (result.HealthScore < _options.MinSystemHealthScore)
                {
                    result.Warnings.Add($"System health score ({result.HealthScore:P1}) is below threshold ({_options.MinSystemHealthScore:P1})");
                }

                // Check for critical areas
                if (insights.HealthScore?.CriticalAreas?.Count > 0)
                {
                    result.Warnings.Add($"Critical areas detected: {string.Join(", ", insights.HealthScore.CriticalAreas)}");
                }

                _logger.LogDebug("AI Optimization Engine health check: {Status}, Score: {Score:P1}", 
                    result.Status, result.HealthScore);
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.Status = "Failed";
                result.Description = $"Health check failed: {ex.Message}";
                result.Exception = ex;
                result.Errors.Add(ex.Message);

                _logger.LogError(ex, "AI Optimization Engine health check failed");
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
