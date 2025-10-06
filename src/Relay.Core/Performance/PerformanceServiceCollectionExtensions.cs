using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using Relay.Core.Configuration.Options;

namespace Relay.Core.Performance
{
    /// <summary>
    /// Service collection extensions for performance optimizations
    /// </summary>
    public static class PerformanceServiceCollectionExtensions
    {
        /// <summary>
        /// Adds performance optimization services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddRelayPerformanceOptimizations(this IServiceCollection services)
        {
            // Add object pool provider if not already registered
            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            // Add telemetry context pool
            services.TryAddSingleton<ITelemetryContextPool, DefaultTelemetryContextPool>();

            // Add buffer manager
            services.TryAddSingleton<IPooledBufferManager>(provider =>
                new DefaultPooledBufferManager(ArrayPool<byte>.Shared));

            // Add pooled telemetry provider
            services.TryAddSingleton<PooledTelemetryProvider>();

            return services;
        }

        /// <summary>
        /// Configures Relay for maximum performance with the specified profile
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="profile">The performance profile to use</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection WithPerformanceProfile(this IServiceCollection services, PerformanceProfile profile)
        {
            services.Configure<RelayOptions>(options =>
            {
                options.Performance.Profile = profile;
            });

            return services;
        }

        /// <summary>
        /// Configures Relay performance options
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configure">Configuration action for performance options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection ConfigurePerformance(this IServiceCollection services, Action<PerformanceOptions> configure)
        {
            services.Configure<RelayOptions>(options =>
            {
                options.Performance.Profile = PerformanceProfile.Custom;
                configure(options.Performance);
            });

            return services;
        }
    }
}