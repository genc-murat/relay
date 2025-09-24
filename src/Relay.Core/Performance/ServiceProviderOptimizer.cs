using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.Performance;

/// <summary>
/// Optimizes service provider calls by caching service instances and factory delegates
/// </summary>
public static class ServiceProviderOptimizer
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object?>> ServiceFactoryCache = new();
    private static readonly ConcurrentDictionary<Type, object?> SingletonCache = new();

    /// <summary>
    /// High-performance service resolution with caching
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOptimizedService<T>(this IServiceProvider provider) where T : class
    {
        var serviceType = typeof(T);

        // Check singleton cache first
        if (SingletonCache.TryGetValue(serviceType, out var cachedService))
        {
            return (T?)cachedService;
        }

        // Use cached factory if available
        var factory = ServiceFactoryCache.GetOrAdd(serviceType, static type =>
        {
            // Create optimized factory delegate
            return sp => sp.GetService(type);
        });

        var service = (T?)factory(provider);

        // Cache singletons for subsequent calls
        if (service != null && IsSingletonService(serviceType))
        {
            SingletonCache.TryAdd(serviceType, service);
        }

        return service;
    }

    /// <summary>
    /// High-performance service resolution for non-generic types
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GetOptimizedService(this IServiceProvider provider, Type serviceType)
    {
        // Check singleton cache first
        if (SingletonCache.TryGetValue(serviceType, out var cachedService))
        {
            return cachedService;
        }

        // Use cached factory if available
        var factory = ServiceFactoryCache.GetOrAdd(serviceType, static type =>
        {
            return sp => sp.GetService(type);
        });

        var service = factory(provider);

        // Cache singletons for subsequent calls
        if (service != null && IsSingletonService(serviceType))
        {
            SingletonCache.TryAdd(serviceType, service);
        }

        return service;
    }

    /// <summary>
    /// Clears all cached services and factories
    /// </summary>
    public static void ClearCache()
    {
        ServiceFactoryCache.Clear();
        SingletonCache.Clear();
    }

    /// <summary>
    /// Checks if a service is registered as singleton (heuristic)
    /// </summary>
    private static bool IsSingletonService(Type serviceType)
    {
        // Simple heuristic - assume handlers and core services are singleton/scoped
        // In a real implementation, this could be more sophisticated
        return serviceType.Name.EndsWith("Handler") ||
               serviceType.Name.EndsWith("Service") ||
               serviceType.Name.EndsWith("Repository");
    }
}