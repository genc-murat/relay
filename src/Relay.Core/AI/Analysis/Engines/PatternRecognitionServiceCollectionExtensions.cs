using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.Engines;

namespace Relay.Core.AI.Analysis.Engines
{
    /// <summary>
    /// Extension methods for registering pattern recognition services
    /// </summary>
    public static class PatternRecognitionServiceCollectionExtensions
    {
        /// <summary>
        /// Adds pattern recognition services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPatternRecognition(this IServiceCollection services)
        {
            // Register configuration
            services.TryAddSingleton<PatternRecognitionConfig>();

            // Register analyzer
            services.TryAddSingleton<IPatternAnalyzer>(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultPatternAnalyzer>();
                var config = sp.GetRequiredService<PatternRecognitionConfig>();
                return new DefaultPatternAnalyzer(logger, config);
            });

            // Register updaters as transient services
            services.AddTransient<RequestTypePatternUpdater>();
            services.AddTransient<StrategyEffectivenessPatternUpdater>();
            services.AddTransient<TemporalPatternUpdater>();
            services.AddTransient<LoadBasedPatternUpdater>();
            services.AddTransient<FeatureImportancePatternUpdater>();
            services.AddTransient<CorrelationPatternUpdater>();
            services.AddTransient<DecisionBoundaryOptimizer>();
            services.AddTransient<EnsembleWeightsUpdater>();
            services.AddTransient<PatternValidator>();

            // Register the main engine
            services.TryAddSingleton<PatternRecognitionEngine>(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<PatternRecognitionEngine>();
                var analyzer = sp.GetRequiredService<IPatternAnalyzer>();
                var updaters = new List<IPatternUpdater>
                {
                    sp.GetRequiredService<RequestTypePatternUpdater>(),
                    sp.GetRequiredService<StrategyEffectivenessPatternUpdater>(),
                    sp.GetRequiredService<TemporalPatternUpdater>(),
                    sp.GetRequiredService<LoadBasedPatternUpdater>(),
                    sp.GetRequiredService<FeatureImportancePatternUpdater>(),
                    sp.GetRequiredService<CorrelationPatternUpdater>(),
                    sp.GetRequiredService<DecisionBoundaryOptimizer>(),
                    sp.GetRequiredService<EnsembleWeightsUpdater>(),
                    sp.GetRequiredService<PatternValidator>()
                };
                var config = sp.GetRequiredService<PatternRecognitionConfig>();
                return new PatternRecognitionEngine(logger, analyzer, updaters, config);
            });

            return services;
        }

        /// <summary>
        /// Adds pattern recognition services with custom configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPatternRecognition(
            this IServiceCollection services,
            Action<PatternRecognitionConfig> configure)
        {
            services.Configure(configure);
            return services.AddPatternRecognition();
        }
    }
}