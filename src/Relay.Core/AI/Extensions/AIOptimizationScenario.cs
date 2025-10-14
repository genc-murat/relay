using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// AI optimization scenarios for preconfigured setups.
    /// </summary>
    public enum AIOptimizationScenario
    {
        /// <summary>Optimized for high throughput applications</summary>
        HighThroughput,
        
        /// <summary>Optimized for low latency requirements</summary>
        LowLatency,
        
        /// <summary>Optimized for resource-constrained environments</summary>
        ResourceConstrained,
        
        /// <summary>Optimized for development environments</summary>
        Development,
        
        /// <summary>Optimized for production environments</summary>
        Production
    }

    /// <summary>
    /// Extension methods for AIOptimizationScenario.
    /// </summary>
    public static class AIOptimizationScenarioExtensions
    {
        /// <summary>
        /// Gets the default optimization options configuration for the specified scenario.
        /// </summary>
        /// <param name="scenario">The optimization scenario</param>
        /// <returns>The action to configure the default options for the scenario</returns>
        public static Action<AIOptimizationOptions> GetDefaultOptions(this AIOptimizationScenario scenario)
        {
            return scenario switch
            {
                AIOptimizationScenario.HighThroughput => options =>
                {
                    options.DefaultBatchSize = 50;
                    options.MaxBatchSize = 200;
                    options.EnableAutomaticOptimization = true;
                    options.MaxAutomaticOptimizationRisk = RiskLevel.Medium;
                    options.ModelUpdateInterval = TimeSpan.FromMinutes(15);
                },

                AIOptimizationScenario.LowLatency => options =>
                {
                    options.DefaultBatchSize = 5;
                    options.MaxBatchSize = 20;
                    options.EnableAutomaticOptimization = true;
                    options.MaxAutomaticOptimizationRisk = RiskLevel.Low;
                    options.ModelUpdateInterval = TimeSpan.FromMinutes(5);
                    options.MinConfidenceScore = 0.85;
                },

                AIOptimizationScenario.ResourceConstrained => options =>
                {
                    options.DefaultBatchSize = 10;
                    options.MaxBatchSize = 30;
                    options.EnableAutomaticOptimization = false;
                    options.ModelUpdateInterval = TimeSpan.FromHours(1);
                    options.EnableMetricsExport = false;
                    options.MinConfidenceScore = 0.9;
                },

                AIOptimizationScenario.Development => options =>
                {
                    options.LearningEnabled = true;
                    options.EnableDecisionLogging = true;
                    options.EnableAutomaticOptimization = false;
                    options.ModelUpdateInterval = TimeSpan.FromMinutes(10);
                    options.EnableMetricsExport = true;
                },

                AIOptimizationScenario.Production => options =>
                {
                    options.LearningEnabled = true;
                    options.EnableAutomaticOptimization = true;
                    options.MaxAutomaticOptimizationRisk = RiskLevel.Low;
                    options.ModelUpdateInterval = TimeSpan.FromMinutes(30);
                    options.EnableHealthMonitoring = true;
                    options.EnableBottleneckDetection = true;
                    options.MinConfidenceScore = 0.8;
                },

                _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown optimization scenario")
            };
        }
    }
}