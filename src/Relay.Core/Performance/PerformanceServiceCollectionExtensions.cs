using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using System.Buffers;

namespace Relay.Core.Performance;

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
    /// Adds performance optimization services with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureObjectPool">Optional object pool provider configuration</param>
    /// <param name="configureArrayPool">Optional array pool configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayPerformanceOptimizations(
        this IServiceCollection services,
        ObjectPoolProvider? configureObjectPool = null,
        ArrayPool<byte>? configureArrayPool = null)
    {
        // Add custom object pool provider if provided
        if (configureObjectPool != null)
        {
            services.TryAddSingleton(configureObjectPool);
        }
        else
        {
            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        }

        // Add telemetry context pool
        services.TryAddSingleton<ITelemetryContextPool, DefaultTelemetryContextPool>();

        // Add buffer manager with custom array pool if provided
        var arrayPool = configureArrayPool ?? ArrayPool<byte>.Shared;
        services.TryAddSingleton<IPooledBufferManager>(provider =>
            new DefaultPooledBufferManager(arrayPool));

        return services;
    }
}