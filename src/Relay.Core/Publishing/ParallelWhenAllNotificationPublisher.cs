using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Publishing
{
    /// <summary>
    /// Parallel notification publisher that continues execution even if handlers throw exceptions.
    /// All handlers run in parallel and all complete regardless of exceptions.
    /// Any exceptions are collected and thrown as an AggregateException.
    /// This ensures all handlers get a chance to execute even if some fail.
    /// </summary>
    public class ParallelWhenAllNotificationPublisher : INotificationPublisher
    {
        private readonly ILogger<ParallelWhenAllNotificationPublisher>? _logger;
        private readonly bool _continueOnException;

        /// <summary>
        /// Initializes a new instance of the ParallelWhenAllNotificationPublisher class.
        /// </summary>
        /// <param name="continueOnException">Whether to continue executing handlers when exceptions occur.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        public ParallelWhenAllNotificationPublisher(
            bool continueOnException = true,
            ILogger<ParallelWhenAllNotificationPublisher>? logger = null)
        {
            _continueOnException = continueOnException;
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
                "Publishing notification {NotificationType} to {HandlerCount} handler(s) in parallel (continue on exception: {ContinueOnException})",
                typeof(TNotification).Name,
                handlersList.Count,
                _continueOnException);

            if (_continueOnException)
            {
                await PublishWithExceptionHandlingAsync(notification, handlersList, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Standard Task.WhenAll - fails fast on first exception
                var tasks = handlersList
                    .Select(handler => handler.HandleAsync(notification, cancellationToken).AsTask())
                    .ToArray();

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            _logger?.LogDebug(
                "All handlers completed for notification {NotificationType}",
                typeof(TNotification).Name);
        }

        private async ValueTask PublishWithExceptionHandlingAsync<TNotification>(
            TNotification notification,
            IList<INotificationHandler<TNotification>> handlers,
            CancellationToken cancellationToken)
            where TNotification : INotification
        {
            var tasks = new List<Task>(handlers.Count);
            var exceptions = new List<Exception>();

            foreach (var handler in handlers)
            {
                var task = Task.Run(async () =>
                {
                    try
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
                    catch (Exception ex)
                    {
                        _logger?.LogError(
                            ex,
                            "Handler {HandlerType} failed while processing notification {NotificationType}",
                            handler.GetType().Name,
                            typeof(TNotification).Name);

                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // If any exceptions occurred, throw them as AggregateException
            if (exceptions.Count > 0)
            {
                _logger?.LogWarning(
                    "{ExceptionCount} handler(s) failed while processing notification {NotificationType}",
                    exceptions.Count,
                    typeof(TNotification).Name);

                throw new AggregateException(
                    $"{exceptions.Count} handler(s) failed while processing notification {typeof(TNotification).Name}",
                    exceptions);
            }
        }
    }
}
