using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.Extensions
{
    /// <summary>
    /// Base class providing common extension method patterns for ServiceCollectionExtensions.
    /// Eliminates code duplication across all Relay service registration extensions.
    /// </summary>
    public static class BaseServiceCollectionExtensions
    {
        /// <summary>
        /// Validates service collection and registers core services with common patterns.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="coreRegistrations">Action to register core services.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection RegisterCoreServices(
            this IServiceCollection services,
            Action<IServiceCollection> coreRegistrations)
        {
            ServiceRegistrationHelper.ValidateServices(services);
            ArgumentNullException.ThrowIfNull(coreRegistrations);
            
            coreRegistrations(services);
            return services;
        }

        /// <summary>
        /// Registers services with configuration options using standard pattern.
        /// </summary>
        /// <typeparam name="TOptions">The options type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action.</param>
        /// <param name="serviceRegistrations">Action to register services that depend on the options.</param>
        /// <param name="postConfigure">Optional post-configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection RegisterWithConfiguration<TOptions>(
            this IServiceCollection services,
            Action<TOptions> configure,
            Action<IServiceCollection>? serviceRegistrations = null,
            Action<TOptions>? postConfigure = null)
            where TOptions : class, new()
        {
            ServiceRegistrationHelper.ValidateServicesAndConfiguration(services, configure);

            // Configure options
            services.Configure(configure);

            // Apply post-configuration if provided
            if (postConfigure != null)
            {
                services.PostConfigure(postConfigure);
            }

            // Register additional services if provided
            serviceRegistrations?.Invoke(services);

            return services;
        }

        /// <summary>
        /// Registers services with configuration from IConfiguration section.
        /// </summary>
        /// <typeparam name="TOptions">The options type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="sectionName">Configuration section name.</param>
        /// <param name="serviceRegistrations">Action to register services that depend on the options.</param>
        /// <param name="postConfigure">Optional post-configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection RegisterWithConfiguration<TOptions>(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName,
            Action<IServiceCollection>? serviceRegistrations = null,
            Action<TOptions>? postConfigure = null)
            where TOptions : class
        {
            ServiceRegistrationHelper.ValidateServices(services);
            ArgumentNullException.ThrowIfNull(configuration);
            
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("Section name cannot be null or whitespace", nameof(sectionName));

            // Bind configuration
            services.Configure<TOptions>(configuration.GetSection(sectionName));
            
            // Apply post-configuration if provided
            if (postConfigure != null)
            {
                services.PostConfigure(postConfigure);
            }
            
            // Register additional services if provided
            serviceRegistrations?.Invoke(services);
            
            return services;
        }

        /// <summary>
        /// Registers multiple pipeline behaviors with common pattern.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="behaviors">Dictionary of behavior interfaces to their implementations.</param>
        /// <param name="lifetime">Service lifetime for behaviors.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection RegisterPipelineBehaviors(
            this IServiceCollection services,
            Dictionary<Type, Type> behaviors,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            ServiceRegistrationHelper.ValidateServices(services);
            ArgumentNullException.ThrowIfNull(behaviors);

            foreach (var behavior in behaviors)
            {
                ServiceRegistrationHelper.TryAddEnumerable(services, behavior.Key, behavior.Value, lifetime);
            }

            return services;
        }

        /// <summary>
        /// Registers a service with multiple implementation options (factory, type, or instance).
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="implementationType">Optional implementation type.</param>
        /// <param name="factory">Optional factory function.</param>
        /// <param name="instance">Optional service instance.</param>
        /// <param name="lifetime">Service lifetime (default: Singleton).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection RegisterServiceWithOptions<TService>(
            this IServiceCollection services,
            Type? implementationType = null,
            Func<IServiceProvider, TService>? factory = null,
            TService? instance = null,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
        {
            ServiceRegistrationHelper.ValidateServices(services);

            // Priority: instance > factory > type
            if (instance != null)
            {
                return ServiceRegistrationHelper.AddServiceInstance(services, instance);
            }
            else if (factory != null)
            {
                return ServiceRegistrationHelper.AddService(services, factory, lifetime);
            }
            else if (implementationType != null)
            {
                if (!typeof(TService).IsAssignableFrom(implementationType))
                {
                    throw new ArgumentException(
                        $"Implementation type {implementationType.Name} must implement {typeof(TService).Name}",
                        nameof(implementationType));
                }
                return ServiceRegistrationHelper.AddService<TService>(services, sp => 
                    (TService)ActivatorUtilities.CreateInstance(sp, implementationType), lifetime);
            }
            else
            {
                throw new ArgumentException(
                    "Either implementationType, factory, or instance must be provided",
                    nameof(implementationType));
            }
        }

        /// <summary>
        /// Registers services with decorator pattern for cross-cutting concerns.
        /// </summary>
        /// <typeparam name="TService">The service type to decorate.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="decoratorType">The decorator type.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection RegisterWithDecorators<TService>(
            this IServiceCollection services,
            Type decoratorType)
            where TService : class
        {
            ServiceRegistrationHelper.ValidateServices(services);
            ArgumentNullException.ThrowIfNull(decoratorType);

            // Register primary decorator using the standard extension method
            services.Decorate(typeof(TService), decoratorType);

            return services;
        }

        /// <summary>
        /// Registers health check services with common pattern.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="healthCheckTypes">Array of health check implementation types.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection RegisterHealthChecks(
            this IServiceCollection services,
            params Type[] healthCheckTypes)
        {
            ServiceRegistrationHelper.ValidateServices(services);
            ArgumentNullException.ThrowIfNull(healthCheckTypes);

            foreach (var healthCheckType in healthCheckTypes)
            {
                services.TryAddSingleton(healthCheckType, healthCheckType);
            }

            return services;
        }

        /// <summary>
        /// Registers services from assembly scanning with common pattern.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="interfaceFilter">Filter for interface types to register.</param>
        /// <param name="lifetime">Service lifetime for registered services.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection RegisterFromAssembly(
            this IServiceCollection services,
            System.Reflection.Assembly assembly,
            Func<Type, bool> interfaceFilter,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            ServiceRegistrationHelper.ValidateServices(services);
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(interfaceFilter);

            var types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces(), (implementation, @interface) => new { implementation, @interface })
                .Where(x => interfaceFilter(x.@interface));

            foreach (var type in types)
            {
                services.Add(new ServiceDescriptor(type.@interface, type.implementation, lifetime));
            }

            return services;
        }

        /// <summary>
        /// Applies conditional service registration based on configuration or environment.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="condition">Condition to evaluate.</param>
        /// <param name="trueRegistrations">Services to register when condition is true.</param>
        /// <param name="falseRegistrations">Optional services to register when condition is false.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection RegisterConditional(
            this IServiceCollection services,
            Func<bool> condition,
            Action<IServiceCollection> trueRegistrations,
            Action<IServiceCollection>? falseRegistrations = null)
        {
            ServiceRegistrationHelper.ValidateServices(services);
            ArgumentNullException.ThrowIfNull(condition);
            ArgumentNullException.ThrowIfNull(trueRegistrations);

            if (condition())
            {
                trueRegistrations(services);
            }
            else
            {
                falseRegistrations?.Invoke(services);
            }

            return services;
        }
    }
}