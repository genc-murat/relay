using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Extension methods for configuring AI optimization services.
    /// </summary>
    public static class AIServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AI optimization services to the service collection.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAIOptimization(this IServiceCollection services)
        {
            return services.AddAIOptimization(_ => { });
        }

        /// <summary>
        /// Adds AI optimization services to the service collection with configuration.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAIOptimization(
            this IServiceCollection services,
            Action<AIOptimizationOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Configure options
            services.Configure(configureOptions);

            // Register core AI services
            services.TryAddSingleton<IAIOptimizationEngine, Relay.Core.AI.AIOptimizationEngine>();
            services.TryAddSingleton<SystemLoadMetricsProvider>();

            // Register pipeline behaviors
            services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(AIOptimizationPipelineBehavior<,>));

            return services;
        }

        /// <summary>
        /// Adds AI optimization services with configuration from IConfiguration.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="sectionName">Configuration section name (default: "Relay:AI")</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAIOptimization(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "Relay:AI")
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("Section name cannot be null or whitespace", nameof(sectionName));

            // Bind configuration
            services.Configure<AIOptimizationOptions>(configuration.GetSection(sectionName));

            // Validate configuration on startup
            services.PostConfigure<AIOptimizationOptions>(options => options.Validate());

            // Register AI services
            return services.AddAIOptimization(_ => { });
        }

        /// <summary>
        /// Adds AI optimization services with enhanced configuration options.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <param name="enableAdvancedFeatures">Whether to enable advanced AI features</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAdvancedAIOptimization(
            this IServiceCollection services,
            Action<AIOptimizationOptions> configureOptions,
            bool enableAdvancedFeatures = true)
        {
            services.AddAIOptimization(configureOptions);

            if (enableAdvancedFeatures)
            {
                // Add advanced AI services
                services.TryAddSingleton<IAIModelTrainer, DefaultAIModelTrainer>();
                services.TryAddSingleton<IAIPredictionCache, DefaultAIPredictionCache>();
                services.TryAddSingleton<IAIMetricsExporter, DefaultAIMetricsExporter>();

                // Add specialized pipeline behaviors
                services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(AIPerformanceTrackingBehavior<,>));
                services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(AIBatchOptimizationBehavior<,>));
                services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(AICachingOptimizationBehavior<,>));
            }

            return services;
        }

        /// <summary>
        /// Configures AI optimization for specific scenarios.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="scenario">The optimization scenario</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAIOptimizationForScenario(
            this IServiceCollection services,
            AIOptimizationScenario scenario)
        {
            return scenario switch
            {
                AIOptimizationScenario.HighThroughput => services.AddAIOptimization(options =>
                {
                    options.DefaultBatchSize = 50;
                    options.MaxBatchSize = 200;
                    options.EnableAutomaticOptimization = true;
                    options.MaxAutomaticOptimizationRisk = RiskLevel.Medium;
                    options.ModelUpdateInterval = TimeSpan.FromMinutes(15);
                }),

                AIOptimizationScenario.LowLatency => services.AddAIOptimization(options =>
                {
                    options.DefaultBatchSize = 5;
                    options.MaxBatchSize = 20;
                    options.EnableAutomaticOptimization = true;
                    options.MaxAutomaticOptimizationRisk = RiskLevel.Low;
                    options.ModelUpdateInterval = TimeSpan.FromMinutes(5);
                    options.MinConfidenceScore = 0.85;
                }),

                AIOptimizationScenario.ResourceConstrained => services.AddAIOptimization(options =>
                {
                    options.DefaultBatchSize = 10;
                    options.MaxBatchSize = 30;
                    options.EnableAutomaticOptimization = false;
                    options.ModelUpdateInterval = TimeSpan.FromHours(1);
                    options.EnableMetricsExport = false;
                    options.MinConfidenceScore = 0.9;
                }),

                AIOptimizationScenario.Development => services.AddAIOptimization(options =>
                {
                    options.LearningEnabled = true;
                    options.EnableDecisionLogging = true;
                    options.EnableAutomaticOptimization = false;
                    options.ModelUpdateInterval = TimeSpan.FromMinutes(10);
                    options.EnableMetricsExport = true;
                }),

                AIOptimizationScenario.Production => services.AddAIOptimization(options =>
                {
                    options.LearningEnabled = true;
                    options.EnableAutomaticOptimization = true;
                    options.MaxAutomaticOptimizationRisk = RiskLevel.Low;
                    options.ModelUpdateInterval = TimeSpan.FromMinutes(30);
                    options.EnableHealthMonitoring = true;
                    options.EnableBottleneckDetection = true;
                    options.MinConfidenceScore = 0.8;
                }),

                _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown optimization scenario")
            };
        }

        /// <summary>
        /// Adds AI optimization with custom prediction models.
        /// </summary>
        /// <typeparam name="TPredictionModel">The custom prediction model type</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAIOptimizationWithCustomModel<TPredictionModel>(
            this IServiceCollection services,
            Action<AIOptimizationOptions> configureOptions)
            where TPredictionModel : class, IAIPredictionModel
        {
            services.AddAIOptimization(configureOptions);
            services.TryAddSingleton<IAIPredictionModel, TPredictionModel>();

            return services;
        }

        /// <summary>
        /// Adds health checks for AI optimization services.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAIOptimizationHealthChecks(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register AI health check services
            services.TryAddSingleton<AIOptimizationHealthCheck>();
            services.TryAddSingleton<AIModelHealthCheck>();
            services.TryAddSingleton<AIMetricsHealthCheck>();
            services.TryAddSingleton<AICircuitBreakerHealthCheck>();
            services.TryAddSingleton<AISystemHealthCheck>();

            return services;
        }

        /// <summary>
        /// Adds comprehensive AI optimization health checks with custom thresholds.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureHealthChecks">Health check configuration action</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAIOptimizationHealthChecks(
            this IServiceCollection services,
            Action<AIHealthCheckOptions> configureHealthChecks)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureHealthChecks == null)
                throw new ArgumentNullException(nameof(configureHealthChecks));

            // Configure health check options
            services.Configure(configureHealthChecks);

            // Register health check services
            services.AddAIOptimizationHealthChecks();

            return services;
        }

        /// <summary>
        /// Gets a comprehensive health status for all AI optimization components.
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive health check result</returns>
        public static async Task<AIHealthCheckResult> GetAIOptimizationHealthAsync(
            this IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            var results = new List<ComponentHealthResult>();
            var overallHealthy = true;
            var startTime = DateTime.UtcNow;

            try
            {
                // Check AI Optimization Engine
                var optimizationCheck = serviceProvider.GetService<AIOptimizationHealthCheck>();
                if (optimizationCheck != null)
                {
                    var result = await optimizationCheck.CheckHealthAsync(cancellationToken);
                    results.Add(result);
                    overallHealthy &= result.IsHealthy;
                }

                // Check AI Model Health
                var modelCheck = serviceProvider.GetService<AIModelHealthCheck>();
                if (modelCheck != null)
                {
                    var result = await modelCheck.CheckHealthAsync(cancellationToken);
                    results.Add(result);
                    overallHealthy &= result.IsHealthy;
                }

                // Check AI Metrics Health
                var metricsCheck = serviceProvider.GetService<AIMetricsHealthCheck>();
                if (metricsCheck != null)
                {
                    var result = await metricsCheck.CheckHealthAsync(cancellationToken);
                    results.Add(result);
                    overallHealthy &= result.IsHealthy;
                }

                // Check Circuit Breaker Health
                var circuitBreakerCheck = serviceProvider.GetService<AICircuitBreakerHealthCheck>();
                if (circuitBreakerCheck != null)
                {
                    var result = await circuitBreakerCheck.CheckHealthAsync(cancellationToken);
                    results.Add(result);
                    overallHealthy &= result.IsHealthy;
                }

                // Check System Health
                var systemCheck = serviceProvider.GetService<AISystemHealthCheck>();
                if (systemCheck != null)
                {
                    var result = await systemCheck.CheckHealthAsync(cancellationToken);
                    results.Add(result);
                    overallHealthy &= result.IsHealthy;
                }

                var duration = DateTime.UtcNow - startTime;

                return new AIHealthCheckResult
                {
                    IsHealthy = overallHealthy,
                    Timestamp = DateTime.UtcNow,
                    Duration = duration,
                    ComponentResults = results,
                    Summary = GenerateHealthSummary(results, overallHealthy)
                };
            }
            catch (Exception ex)
            {
                return new AIHealthCheckResult
                {
                    IsHealthy = false,
                    Timestamp = DateTime.UtcNow,
                    Duration = DateTime.UtcNow - startTime,
                    ComponentResults = results,
                    Summary = $"Health check failed with exception: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private static string GenerateHealthSummary(List<ComponentHealthResult> results, bool overallHealthy)
        {
            var healthyCount = results.Count(r => r.IsHealthy);
            var totalCount = results.Count;
            var status = overallHealthy ? "Healthy" : "Unhealthy";

            return $"AI Optimization Status: {status} ({healthyCount}/{totalCount} components healthy)";
        }
    }
}