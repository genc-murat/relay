using System.Linq;
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
            // Register core interfaces if not already registered
            if (!services.Any(d => d.ServiceType == typeof(IMetricsPublisher)))
                services.AddSingleton<IMetricsPublisher, DefaultMetricsPublisher>();
            
            if (!services.Any(d => d.ServiceType == typeof(IMetricsAggregator)))
                services.AddSingleton<IMetricsAggregator, DefaultMetricsAggregator>();
            
            if (!services.Any(d => d.ServiceType == typeof(ISystemAnalyzer)))
                services.AddSingleton<ISystemAnalyzer, DefaultSystemAnalyzer>();
            
            if (!services.Any(d => d.ServiceType == typeof(IHealthScorer)))
                services.AddSingleton<IHealthScorer, CompositeHealthScorer>();

            // Register individual health scorers if not already registered
            if (!services.Any(d => d.ServiceType == typeof(PerformanceScorer)))
                services.AddSingleton<PerformanceScorer>();
            
            if (!services.Any(d => d.ServiceType == typeof(ReliabilityScorer)))
                services.AddSingleton<ReliabilityScorer>();
            
            if (!services.Any(d => d.ServiceType == typeof(ScalabilityScorer)))
                services.AddSingleton<ScalabilityScorer>();
            
            if (!services.Any(d => d.ServiceType == typeof(SecurityScorer)))
                services.AddSingleton<SecurityScorer>();
            
            if (!services.Any(d => d.ServiceType == typeof(MaintainabilityScorer)))
                services.AddSingleton<MaintainabilityScorer>();

            // Register options if not already registered
            if (!services.Any(d => d.ServiceType == typeof(MetricsCollectionOptions)))
                services.AddSingleton<MetricsCollectionOptions>();
            
            if (!services.Any(d => d.ServiceType == typeof(HealthScoringOptions)))
                services.AddSingleton<HealthScoringOptions>();

            // Register the main service if not already registered
            if (!services.Any(d => d.ServiceType == typeof(SystemMetricsService)))
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
            // Register custom options if not already registered
            if (!services.Any(d => d.ServiceType == typeof(MetricsCollectionOptions)))
                services.AddSingleton(metricsOptions);
            
            if (!services.Any(d => d.ServiceType == typeof(HealthScoringOptions)))
                services.AddSingleton(healthOptions);

            return services.AddMetricsServices();
        }
    }
}