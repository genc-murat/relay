using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Publishing
{
    /// <summary>
    /// Sequential notification publisher that executes handlers one at a time in order.
    /// If a handler throws an exception, execution stops and the exception propagates.
    /// This is the safest strategy but slowest for multiple handlers.
    /// </summary>
    public class SequentialNotificationPublisher : INotificationPublisher
    {
        private readonly ILogger<SequentialNotificationPublisher>? _logger;

        /// <summary>
        /// Initializes a new instance of the SequentialNotificationPublisher class.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        public SequentialNotificationPublisher(ILogger<SequentialNotificationPublisher>? logger = null)
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

            _logger?.LogDebug(
                "Publishing notification {NotificationType} to {HandlerCount} handler(s) sequentially",
                typeof(TNotification).Name,
                handlersList.Count);

            foreach (var handler in handlersList)
            {
                _logger?.LogTrace(
                    "Executing handler {HandlerType} for notification {NotificationType}",
                    handler.GetType().Name,
                    typeof(TNotification).Name);

                await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);

                _logger?.LogTrace(
                    "Handler {HandlerType} completed successfully",
                    handler.GetType().Name);
            }

            _logger?.LogDebug(
                "All handlers completed for notification {NotificationType}",
                typeof(TNotification).Name);
        }
    }
}
