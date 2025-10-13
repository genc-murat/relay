using System;
using Relay.Core.Publishing.Strategies;

namespace Relay.Core.Publishing.Options
{
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
