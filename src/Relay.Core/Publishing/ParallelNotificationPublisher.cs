using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Publishing
{
    /// <summary>
    /// Parallel notification publisher that executes all handlers concurrently.
    /// All handlers run in parallel and the method waits for all to complete.
    /// If any handler throws an exception, it will be propagated after all handlers complete.
    /// This is the fastest strategy but requires handlers to be thread-safe.
    /// </summary>
    public class ParallelNotificationPublisher : INotificationPublisher
    {
        private readonly ILogger<ParallelNotificationPublisher>? _logger;

        /// <summary>
        /// Initializes a new instance of the ParallelNotificationPublisher class.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        public ParallelNotificationPublisher(ILogger<ParallelNotificationPublisher>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async ValueTask PublishAsync<TNotification>(
            TNotification notification,
            IEnumerable<INotificationHandler<TNotification>> handlers,
            CancellationToken cancellationToken)
            where TNotification : INotification
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));
            if (handlers == null)
                throw new ArgumentNullException(nameof(handlers));

            var handlersList = handlers as IList<INotificationHandler<TNotification>> ?? new List<INotificationHandler<TNotification>>(handlers);

            if (handlersList.Count == 0)
            {
                _logger?.LogDebug(
                    "No handlers registered for notification {NotificationType}",
                    typeof(TNotification).Name);
                return;
            }

            _logger?.LogDebug(
                "Publishing notification {NotificationType} to {HandlerCount} handler(s) in parallel",
                typeof(TNotification).Name,
                handlersList.Count);

            // Optimize for single handler
            if (handlersList.Count == 1)
            {
                await handlersList[0].HandleAsync(notification, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Execute all handlers in parallel using Task.WhenAll
            // Use Task.Run to ensure handlers start executing concurrently
            var tasks = new Task[handlersList.Count];
            for (int i = 0; i < handlersList.Count; i++)
            {
                var handler = handlersList[i];
                _logger?.LogTrace(
                    "Starting handler {HandlerType} for notification {NotificationType}",
                    handler.GetType().Name,
                    typeof(TNotification).Name);

                tasks[i] = Task.Run(async () => await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false), cancellationToken);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            _logger?.LogDebug(
                "All handlers completed for notification {NotificationType}",
                typeof(TNotification).Name);
        }
    }
}
