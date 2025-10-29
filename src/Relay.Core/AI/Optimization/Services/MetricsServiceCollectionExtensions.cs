using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Extension methods for registering metrics services
    /// </summary>
    public static class MetricsServiceCollectionExtensions
    {
        /// <summary>
        /// Add metrics services to the service collection
        /// </summary>
        public static IServiceCollection AddMetricsServices(this IServiceCollection services)
        {
            // Register core interfaces
            services.AddSingleton<IMetricsPublisher, DefaultMetricsPublisher>();
            services.AddSingleton<IMetricsAggregator, DefaultMetricsAggregator>();
            services.AddSingleton<ISystemAnalyzer, DefaultSystemAnalyzer>();
            services.AddSingleton<IHealthScorer, CompositeHealthScorer>();

            // Register individual health scorers
            services.AddSingleton<PerformanceScorer>();
            services.AddSingleton<ReliabilityScorer>();
            services.AddSingleton<ScalabilityScorer>();
            services.AddSingleton<SecurityScorer>();
            services.AddSingleton<MaintainabilityScorer>();

            // Register options
            services.AddSingleton<MetricsCollectionOptions>();
            services.AddSingleton<HealthScoringOptions>();

            // Register the main service
            services.AddSingleton<SystemMetricsService>();

            return services;
        }

        /// <summary>
        /// Add metrics services with custom options
        /// </summary>
        public static IServiceCollection AddMetricsServices(
            this IServiceCollection services,
            MetricsCollectionOptions metricsOptions,
            HealthScoringOptions healthOptions)
        {
            services.AddSingleton(metricsOptions);
            services.AddSingleton(healthOptions);

            return services.AddMetricsServices();
        }
    }
}