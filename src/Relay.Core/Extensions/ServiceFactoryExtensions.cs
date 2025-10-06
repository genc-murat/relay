using Relay.Core.Contracts.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.Extensions
{
    /// <summary>
    /// Extension methods for ServiceFactory to provide type-safe service resolution.
    /// These extensions make it easier to resolve services from the DI container.
    /// </summary>
    public static class ServiceFactoryExtensions
    {
        /// <summary>
        /// Resolves a service of the specified type.
        /// </summary>
        /// <typeparam name="TService">The type of service to resolve.</typeparam>
        /// <param name="factory">The service factory.</param>
        /// <returns>An instance of the requested service, or null if not registered.</returns>
        public static TService? GetService<TService>(this ServiceFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return (TService?)factory(typeof(TService));
        }

        /// <summary>
        /// Resolves a required service of the specified type.
        /// Throws an exception if the service is not registered.
        /// </summary>
        /// <typeparam name="TService">The type of service to resolve.</typeparam>
        /// <param name="factory">The service factory.</param>
        /// <returns>An instance of the requested service.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
        public static TService GetRequiredService<TService>(this ServiceFactory factory)
            where TService : notnull
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var service = factory(typeof(TService));
            if (service == null)
            {
                throw new InvalidOperationException(
                    $"Required service of type '{typeof(TService).FullName}' is not registered in the DI container.");
            }

            return (TService)service;
        }

        /// <summary>
        /// Resolves all services of the specified type.
        /// </summary>
        /// <typeparam name="TService">The type of services to resolve.</typeparam>
        /// <param name="factory">The service factory.</param>
        /// <returns>An enumerable of all registered services of the specified type.</returns>
        public static IEnumerable<TService> GetServices<TService>(this ServiceFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var services = factory(typeof(IEnumerable<TService>)) as IEnumerable<TService>;
            return services ?? Enumerable.Empty<TService>();
        }

        /// <summary>
        /// Attempts to resolve a service of the specified type.
        /// </summary>
        /// <typeparam name="TService">The type of service to resolve.</typeparam>
        /// <param name="factory">The service factory.</param>
        /// <param name="service">When this method returns, contains the resolved service, or null if not found.</param>
        /// <returns>true if the service was resolved; otherwise, false.</returns>
        public static bool TryGetService<TService>(this ServiceFactory factory, out TService? service)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            try
            {
                var result = factory(typeof(TService));
                if (result is TService typedResult)
                {
                    service = typedResult;
                    return true;
                }
            }
            catch
            {
                // Swallow exceptions and return false
            }

            service = default;
            return false;
        }

        /// <summary>
        /// Creates a scoped service factory from an IServiceProvider.
        /// This is useful when you need to create a new scope for service resolution.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>A service factory that resolves services from a new scope.</returns>
        public static ServiceFactory CreateScopedFactory(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            return serviceType => serviceProvider.GetService(serviceType);
        }
    }
}
