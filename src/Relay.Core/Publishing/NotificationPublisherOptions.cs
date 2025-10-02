using System;

namespace Relay.Core.Publishing
{
    /// <summary>
    /// Configuration options for notification publishing strategy.
    /// </summary>
    public class NotificationPublisherOptions
    {
        /// <summary>
        /// Gets or sets the notification publisher type to use.
        /// If not set, SequentialNotificationPublisher is used by default.
        /// </summary>
        public Type? PublisherType { get; set; }

        /// <summary>
        /// Gets or sets the notification publisher instance to use.
        /// If set, this takes precedence over PublisherType.
        /// </summary>
        public INotificationPublisher? Publisher { get; set; }

        /// <summary>
        /// Gets or sets whether to continue executing handlers when exceptions occur.
        /// Only applies to certain publisher strategies like ParallelWhenAllNotificationPublisher.
        /// Default is true.
        /// </summary>
        public bool ContinueOnException { get; set; } = true;

        /// <summary>
        /// Gets or sets the service lifetime for the notification publisher.
        /// Default is Singleton.
        /// </summary>
        public Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; set; }
            = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton;
    }

    /// <summary>
    /// Predefined notification publishing strategies.
    /// </summary>
    public static class NotificationPublishingStrategy
    {
        /// <summary>
        /// Sequential publishing - handlers execute one at a time in order.
        /// Stops on first exception. Safest but slowest.
        /// </summary>
        public static Type Sequential => typeof(SequentialNotificationPublisher);

        /// <summary>
        /// Parallel publishing - handlers execute concurrently using Task.WhenAll.
        /// Stops on first exception. Fast but requires thread-safe handlers.
        /// </summary>
        public static Type Parallel => typeof(ParallelNotificationPublisher);

        /// <summary>
        /// Parallel publishing with exception tolerance - handlers execute concurrently.
        /// Continues execution even if handlers fail. All exceptions are collected.
        /// Ensures all handlers get a chance to execute.
        /// </summary>
        public static Type ParallelWhenAll => typeof(ParallelWhenAllNotificationPublisher);
    }
}
