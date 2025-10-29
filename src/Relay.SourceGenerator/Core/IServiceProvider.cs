using System;
using System.Collections.Generic;

namespace Relay.SourceGenerator.Core;

/// <summary>
/// Service provider interface for dependency injection within the source generator.
/// Follows the Dependency Inversion Principle.
/// </summary>
public interface IServiceProvider
{
    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="TService">The type of service to retrieve</typeparam>
    /// <returns>The service instance</returns>
    TService GetService<TService>() where TService : class;

    /// <summary>
    /// Gets a service of the specified type, or null if not found.
    /// </summary>
    /// <typeparam name="TService">The type of service to retrieve</typeparam>
    /// <returns>The service instance, or null if not found</returns>
    TService? GetServiceOrNull<TService>() where TService : class;

    /// <summary>
    /// Registers a service instance.
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="instance">The service instance</param>
    void RegisterService<TService>(TService instance) where TService : class;

    /// <summary>
    /// Registers a service factory.
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="factory">The factory function</param>
    void RegisterService<TService>(Func<IServiceProvider, TService> factory) where TService : class;
}

/// <summary>
/// Simple service provider implementation for the source generator.
/// </summary>
public class SimpleServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<Type, Func<IServiceProvider, object>> _factories = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public TService GetService<TService>() where TService : class
    {
        var service = GetServiceOrNull<TService>();
        if (service == null)
        {
            throw new InvalidOperationException($"Service of type {typeof(TService).Name} is not registered.");
        }
        return service;
    }

    /// <inheritdoc/>
    public TService? GetServiceOrNull<TService>() where TService : class
    {
        var serviceType = typeof(TService);

        lock (_lock)
        {
            // Check if instance already exists
            if (_services.TryGetValue(serviceType, out var instance))
            {
                return instance as TService;
            }

            // Check if factory exists
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                var newInstance = factory(this) as TService;
                if (newInstance != null)
                {
                    _services[serviceType] = newInstance;
                }
                return newInstance;
            }

            return null;
        }
    }

    /// <inheritdoc/>
    public void RegisterService<TService>(TService instance) where TService : class
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));

        var serviceType = typeof(TService);
        lock (_lock)
        {
            _services[serviceType] = instance;
        }
    }

    /// <inheritdoc/>
    public void RegisterService<TService>(Func<IServiceProvider, TService> factory) where TService : class
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var serviceType = typeof(TService);
        lock (_lock)
        {
            _factories[serviceType] = sp => factory(sp);
        }
    }

    /// <summary>
    /// Clears all registered services and factories.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _services.Clear();
            _factories.Clear();
        }
    }
}
