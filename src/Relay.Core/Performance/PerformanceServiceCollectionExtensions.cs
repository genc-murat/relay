using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using Relay.Core.Configuration.Options;
using Relay.Core.Performance.BufferManagement;
using Relay.Core.Performance.Telemetry;
using Relay.Core.Extensions;

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
            return services.RegisterCoreServices(svc =>
            {
                // Add object pool provider if not already registered
                ServiceRegistrationHelper.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>(svc);

                // Add telemetry context pool
                ServiceRegistrationHelper.TryAddSingleton<ITelemetryContextPool, DefaultTelemetryContextPool>(svc);

                // Add buffer manager
                ServiceRegistrationHelper.TryAddSingleton(svc, provider =>
                    new DefaultPooledBufferManager(ArrayPool<byte>.Shared));

            // Add pooled telemetry provider
            svc.TryAddSingleton<PooledTelemetryProvider>();
            });
        }

        /// <summary>
        /// Configures Relay for maximum performance with the specified profile
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="profile">The performance profile to use</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection WithPerformanceProfile(this IServiceCollection services, PerformanceProfile profile)
        {
            return ServiceRegistrationHelper.ConfigureOptions<RelayOptions>(services, options => options.Performance.Profile = profile);
        }

        /// <summary>
        /// Configures Relay performance options
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configure">Configuration action for performance options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection ConfigurePerformance(this IServiceCollection services, Action<PerformanceOptions> configure)
        {
            return ServiceRegistrationHelper.ConfigureOptions<RelayOptions>(services, options =>
            {
                options.Performance.Profile = PerformanceProfile.Custom;
                configure(options.Performance);
            });
        }
    }
}