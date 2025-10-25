using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.Extensions;

/// <summary>
/// Helper class for common service registration patterns to eliminate duplication across ServiceCollectionExtensions.
/// </summary>
public static class ServiceRegistrationHelper
{
    /// <summary>
    /// Validates that the service collection is not null.
    /// </summary>
    /// <param name="services">The service collection to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static void ValidateServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
    }

    /// <summary>
    /// Validates that the service collection and configuration action are not null.
    /// </summary>
    /// <param name="services">The service collection to validate.</param>
    /// <param name="configure">The configuration action to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when either parameter is null.</exception>
    public static void ValidateServicesAndConfiguration<T>(IServiceCollection services, Action<T> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
    }

    /// <summary>
    /// Validates that the service collection and factory function are not null.
    /// </summary>
    /// <param name="services">The service collection to validate.</param>
    /// <param name="factory">The factory function to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when either parameter is null.</exception>
    public static void ValidateServicesAndFactory<T>(IServiceCollection services, Func<IServiceProvider, T> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
    }

    /// <summary>
    /// Registers a singleton service if not already registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddSingleton(
        IServiceCollection services,
        Type serviceType,
        Type implementationType)
    {
        ValidateServices(services);
        services.TryAddSingleton(serviceType, implementationType);
        return services;
    }

    /// <summary>
    /// Registers a singleton service if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddSingleton<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        ValidateServices(services);
        services.TryAddSingleton<TService, TImplementation>();
        return services;
    }

    /// <summary>
    /// Registers a singleton service with a factory if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The factory function.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddSingleton<TService>(IServiceCollection services, Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ValidateServicesAndFactory(services, factory);
        services.TryAddSingleton(factory);
        return services;
    }

    /// <summary>
    /// Registers a transient service if not already registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddTransient(
        IServiceCollection services,
        Type serviceType,
        Type implementationType)
    {
        ValidateServices(services);
        services.TryAddTransient(serviceType, implementationType);
        return services;
    }

    /// <summary>
    /// Registers a transient service if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddTransient<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        ValidateServices(services);
        services.TryAddTransient<TService, TImplementation>();
        return services;
    }

    /// <summary>
    /// Registers a transient service with a factory if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The factory function.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddTransient<TService>(IServiceCollection services, Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ValidateServicesAndFactory(services, factory);
        services.TryAddTransient(factory);
        return services;
    }

    /// <summary>
    /// Registers a transient service (allows multiple registrations for the same service type).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTransient(
        IServiceCollection services,
        Type serviceType,
        Type implementationType)
    {
        ValidateServices(services);
        services.AddTransient(serviceType, implementationType);
        return services;
    }

    /// <summary>
    /// Registers a transient service (allows multiple registrations for the same service type).
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTransient<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        ValidateServices(services);
        services.AddTransient<TService, TImplementation>();
        return services;
    }

    /// <summary>
    /// Registers an enumerable service (for multiple implementations) if not already registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddEnumerable(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ValidateServices(services);
        services.TryAddEnumerable(new ServiceDescriptor(serviceType, implementationType, lifetime));
        return services;
    }

    /// <summary>
    /// Registers an enumerable service (for multiple implementations) if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddEnumerable<TService, TImplementation>(
        IServiceCollection services, 
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TService : class
        where TImplementation : class, TService
    {
        return TryAddEnumerable(services, typeof(TService), typeof(TImplementation), lifetime);
    }

    /// <summary>
    /// Configures options with validation.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureOptions<TOptions>(IServiceCollection services, Action<TOptions> configure)
        where TOptions : class
    {
        ValidateServicesAndConfiguration(services, configure);
        services.Configure(configure);
        return services;
    }

    /// <summary>
    /// Configures options with default configuration if no configuration is provided.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureOptionsWithDefault<TOptions>(
        IServiceCollection services, 
        Action<TOptions>? configure = null)
        where TOptions : class, new()
    {
        ValidateServices(services);
        
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<TOptions>(options => { });
        }
        
        return services;
    }

    /// <summary>
    /// Registers a service with a specific lifetime and descriptor.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddService<TService, TImplementation>(
        IServiceCollection services, 
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TService : class
        where TImplementation : class, TService
    {
        ValidateServices(services);
        services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
        return services;
    }

    /// <summary>
    /// Registers a service with a factory function and specific lifetime.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The factory function.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddService<TService>(
        IServiceCollection services, 
        Func<IServiceProvider, TService> factory,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TService : class
    {
        ValidateServicesAndFactory(services, factory);
        services.Add(new ServiceDescriptor(typeof(TService), factory, lifetime));
        return services;
    }

    /// <summary>
    /// Registers a service instance with singleton lifetime.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="instance">The service instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceInstance<TService>(
        IServiceCollection services, 
        TService instance)
        where TService : class
    {
        ValidateServices(services);
        ArgumentNullException.ThrowIfNull(instance);
        services.Add(new ServiceDescriptor(typeof(TService), instance));
        return services;
    }

    /// <summary>
    /// Decorates an existing service with another implementation.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection DecorateService<TService, TDecorator>(IServiceCollection services)
        where TService : class
        where TDecorator : class, TService
    {
        ValidateServices(services);
        services.Decorate<TService, TDecorator>();
        return services;
    }

    /// <summary>
    /// Conditionally decorates a service if it is already registered.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryDecorateService<TService, TDecorator>(IServiceCollection services)
        where TService : class
        where TDecorator : class, TService
    {
        ValidateServices(services);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TService)))
        {
            services.Decorate<TService, TDecorator>();
        }
        return services;
    }

    /// <summary>
    /// Registers multiple services of the same type with different lifetimes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="registrations">Dictionary of service types to their implementations and lifetimes.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMultipleServices(
        IServiceCollection services,
        Dictionary<Type, (Type Implementation, ServiceLifetime Lifetime)> registrations)
    {
        ValidateServices(services);
        ArgumentNullException.ThrowIfNull(registrations);

        foreach (var registration in registrations)
        {
            services.Add(new ServiceDescriptor(registration.Key, registration.Value.Implementation, registration.Value.Lifetime));
        }

        return services;
    }

    /// <summary>
    /// Registers a service with conditional logic based on existing registration.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="condition">Condition to check before registration.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddConditional<TService, TImplementation>(
        IServiceCollection services,
        Func<IServiceCollection, bool> condition,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TService : class
        where TImplementation : class, TService
    {
        ValidateServices(services);
        ArgumentNullException.ThrowIfNull(condition);

        if (condition(services))
        {
            services.TryAdd(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
        }

        return services;
    }
}