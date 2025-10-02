using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Relay.Core.Publishing
{
    /// <summary>
    /// Extension methods for configuring notification publishing strategies.
    /// </summary>
    public static class NotificationPublisherServiceCollectionExtensions
    {
        /// <summary>
        /// Configures the notification publishing strategy for Relay.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for notification publisher options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureNotificationPublisher(
            this IServiceCollection services,
            Action<NotificationPublisherOptions> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new NotificationPublisherOptions();
            configure(options);

            // If a specific publisher instance is provided, use it
            if (options.Publisher != null)
            {
                services.TryAdd(new ServiceDescriptor(
                    typeof(INotificationPublisher),
                    options.Publisher));
                return services;
            }

            // If a publisher type is specified, register it
            if (options.PublisherType != null)
            {
                if (!typeof(INotificationPublisher).IsAssignableFrom(options.PublisherType))
                {
                    throw new ArgumentException(
                        $"Publisher type {options.PublisherType.Name} must implement INotificationPublisher",
                        nameof(configure));
                }

                services.TryAdd(new ServiceDescriptor(
                    typeof(INotificationPublisher),
                    options.PublisherType,
                    options.Lifetime));
                return services;
            }

            // Default to SequentialNotificationPublisher
            services.TryAdd(new ServiceDescriptor(
                typeof(INotificationPublisher),
                typeof(SequentialNotificationPublisher),
                options.Lifetime));

            return services;
        }

        /// <summary>
        /// Uses sequential notification publishing strategy.
        /// Handlers execute one at a time in order. Stops on first exception.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Singleton).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection UseSequentialNotificationPublisher(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAdd(new ServiceDescriptor(
                typeof(INotificationPublisher),
                typeof(SequentialNotificationPublisher),
                lifetime));

            return services;
        }

        /// <summary>
        /// Uses parallel notification publishing strategy.
        /// Handlers execute concurrently using Task.WhenAll. Stops on first exception.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Singleton).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection UseParallelNotificationPublisher(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAdd(new ServiceDescriptor(
                typeof(INotificationPublisher),
                typeof(ParallelNotificationPublisher),
                lifetime));

            return services;
        }

        /// <summary>
        /// Uses parallel notification publishing strategy with exception tolerance.
        /// Handlers execute concurrently. Continues execution even if handlers fail.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="continueOnException">Whether to continue executing handlers when exceptions occur.</param>
        /// <param name="lifetime">The service lifetime (default: Singleton).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection UseParallelWhenAllNotificationPublisher(
            this IServiceCollection services,
            bool continueOnException = true,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAdd(new ServiceDescriptor(
                typeof(INotificationPublisher),
                sp => new ParallelWhenAllNotificationPublisher(
                    continueOnException,
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<ParallelWhenAllNotificationPublisher>>()),
                lifetime));

            return services;
        }

        /// <summary>
        /// Uses a custom notification publishing strategy.
        /// </summary>
        /// <typeparam name="TPublisher">The custom publisher implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Singleton).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection UseCustomNotificationPublisher<TPublisher>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TPublisher : class, INotificationPublisher
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAdd(new ServiceDescriptor(
                typeof(INotificationPublisher),
                typeof(TPublisher),
                lifetime));

            return services;
        }

        /// <summary>
        /// Uses a custom notification publishing strategy with a factory function.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="implementationFactory">The factory function to create the publisher.</param>
        /// <param name="lifetime">The service lifetime (default: Singleton).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection UseCustomNotificationPublisher(
            this IServiceCollection services,
            Func<IServiceProvider, INotificationPublisher> implementationFactory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            services.TryAdd(new ServiceDescriptor(
                typeof(INotificationPublisher),
                implementationFactory,
                lifetime));

            return services;
        }
    }
}
