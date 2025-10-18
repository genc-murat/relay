using System;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.AI
{
    /// <summary>
    /// Extension methods for registering trend analysis services
    /// </summary>
    public static class TrendAnalysisServiceCollectionExtensions
    {
        /// <summary>
        /// Adds trend analysis services to the service collection
        /// </summary>
        public static IServiceCollection AddTrendAnalysis(this IServiceCollection services)
        {
            return services.AddTrendAnalysis(_ => { });
        }

        /// <summary>
        /// Adds trend analysis services to the service collection with configuration
        /// </summary>
        public static IServiceCollection AddTrendAnalysis(
            this IServiceCollection services,
            Action<TrendAnalysisConfig> configure)
        {
            services.Configure(configure);

            services.AddSingleton<TrendAnalysisConfig>();
            services.AddTransient<IMovingAverageUpdater, MovingAverageUpdater>();
            services.AddTransient<ITrendDirectionUpdater, TrendDirectionUpdater>();
            services.AddTransient<ITrendVelocityUpdater, TrendVelocityUpdater>();
            services.AddTransient<ISeasonalityUpdater, SeasonalityUpdater>();
            services.AddTransient<IRegressionUpdater, RegressionUpdater>();
            services.AddTransient<ICorrelationUpdater, CorrelationUpdater>();
            services.AddTransient<IAnomalyUpdater, AnomalyUpdater>();
            services.AddTransient<ITrendAnalyzer, TrendAnalyzer>();

            return services;
        }
    }
}