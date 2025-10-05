using System;
using System.Linq;
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
            // Health checks would require additional dependency - simplified for now
            return services;
        }
    }
}