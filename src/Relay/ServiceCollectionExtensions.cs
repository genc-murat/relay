using System;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Configuration;

namespace Relay
{
    /// <summary>
    /// Extension methods for configuring the Relay framework.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Relay services to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelay(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Add core Relay services
            services.AddRelayConfiguration();
            services.AddTransient<IRelay, RelayImplementation>();
            
            // Register default dispatchers (will be replaced by generated ones if available)
            services.AddTransient<IRequestDispatcher, FallbackDispatcher>();
            services.AddTransient<IStreamDispatcher, StreamDispatcher>();
            services.AddTransient<INotificationDispatcher, NotificationDispatcher>();

            return services;
        }
    }
}