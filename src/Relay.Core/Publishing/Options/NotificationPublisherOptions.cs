using System;
using Relay.Core.Publishing.Interfaces;

namespace Relay.Core.Publishing.Options
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
}
