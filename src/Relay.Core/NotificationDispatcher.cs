using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Relay.Core
{
    /// <summary>
    /// Configuration options for notification dispatching.
    /// </summary>
    public class NotificationDispatchOptions
    {
        /// <summary>
        /// Gets or sets the default dispatch mode for notifications.
        /// </summary>
        public NotificationDispatchMode DefaultDispatchMode { get; set; } = NotificationDispatchMode.Parallel;

        /// <summary>
        /// Gets or sets whether to continue execution when a handler throws an exception.
        /// </summary>
        public bool ContinueOnException { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism for parallel dispatch.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    }

    /// <summary>
    /// Represents a notification handler registration with metadata.
    /// </summary>
    public class NotificationHandlerRegistration
    {
        /// <summary>
        /// Gets or sets the notification type.
        /// </summary>
        public Type NotificationType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the handler type.
        /// </summary>
        public Type HandlerType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the dispatch mode for this handler.
        /// </summary>
        public NotificationDispatchMode DispatchMode { get; set; }

        /// <summary>
        /// Gets or sets the priority of this handler. Higher values indicate higher priority.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets the handler factory function.
        /// </summary>
        public Func<IServiceProvider, object> HandlerFactory { get; set; } = null!;

        /// <summary>
        /// Gets or sets the handler execution function.
        /// </summary>
        public Func<object, INotification, CancellationToken, ValueTask> ExecuteHandler { get; set; } = null!;
    }

    /// <summary>
    /// Default implementation of notification dispatcher with configurable dispatch strategies.
    /// </summary>
    public class NotificationDispatcher : BaseNotificationDispatcher
    {
        private readonly NotificationDispatchOptions _options;
        private readonly ILogger<NotificationDispatcher>? _logger;
        private readonly Dictionary<Type, List<NotificationHandlerRegistration>> _handlerRegistrations;

        /// <summary>
        /// Initializes a new instance of the NotificationDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        /// <param name="options">The notification dispatch options.</param>
        /// <param name="logger">The logger for diagnostic information.</param>
        public NotificationDispatcher(
            IServiceProvider serviceProvider,
            NotificationDispatchOptions? options = null,
            ILogger<NotificationDispatcher>? logger = null)
            : base(serviceProvider)
        {
            _options = options ?? new NotificationDispatchOptions();
            _logger = logger;
            _handlerRegistrations = new Dictionary<Type, List<NotificationHandlerRegistration>>();
        }

        /// <summary>
        /// Registers a notification handler.
        /// </summary>
        /// <param name="registration">The handler registration.</param>
        public void RegisterHandler(NotificationHandlerRegistration registration)
        {
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));

            if (!_handlerRegistrations.TryGetValue(registration.NotificationType, out var handlers))
            {
                handlers = new List<NotificationHandlerRegistration>();
                _handlerRegistrations[registration.NotificationType] = handlers;
            }

            handlers.Add(registration);
            
            // Sort by priority (higher priority first)
            handlers.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }

        /// <inheritdoc />
        public override async ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        {
            ValidateRequest(notification, nameof(notification));

            var notificationType = typeof(TNotification);
            
            if (!_handlerRegistrations.TryGetValue(notificationType, out var handlers) || handlers.Count == 0)
            {
                _logger?.LogDebug("No handlers registered for notification type {NotificationType}", notificationType.Name);
                return;
            }

            _logger?.LogDebug("Dispatching notification {NotificationType} to {HandlerCount} handlers", 
                notificationType.Name, handlers.Count);

            // Group handlers by dispatch mode
            var parallelHandlers = handlers.Where(h => h.DispatchMode == NotificationDispatchMode.Parallel).ToList();
            var sequentialHandlers = handlers.Where(h => h.DispatchMode == NotificationDispatchMode.Sequential).ToList();

            // Execute sequential handlers first (in priority order)
            if (sequentialHandlers.Count > 0)
            {
                await ExecuteHandlersSequentially(sequentialHandlers, notification, cancellationToken);
            }

            // Execute parallel handlers concurrently
            if (parallelHandlers.Count > 0)
            {
                await ExecuteHandlersInParallel(parallelHandlers, notification, cancellationToken);
            }
        }

        /// <summary>
        /// Executes handlers sequentially with exception isolation.
        /// </summary>
        private async ValueTask ExecuteHandlersSequentially<TNotification>(
            IEnumerable<NotificationHandlerRegistration> handlers,
            TNotification notification,
            CancellationToken cancellationToken)
            where TNotification : INotification
        {
            foreach (var handlerRegistration in handlers)
            {
                await ExecuteHandlerSafely(handlerRegistration, notification, cancellationToken);
            }
        }

        /// <summary>
        /// Executes handlers in parallel with exception isolation.
        /// </summary>
        private async ValueTask ExecuteHandlersInParallel<TNotification>(
            IList<NotificationHandlerRegistration> handlers,
            TNotification notification,
            CancellationToken cancellationToken)
            where TNotification : INotification
        {
            if (handlers.Count == 1)
            {
                // Optimize for single handler case
                await ExecuteHandlerSafely(handlers[0], notification, cancellationToken);
                return;
            }

            // Create tasks for parallel execution on the thread pool to avoid SynchronizationContext serialization
            var tasks = new Task[handlers.Count];
            for (int i = 0; i < handlers.Count; i++)
            {
                var handler = handlers[i];
                tasks[i] = Task.Run(() => ExecuteHandlerSafely(handler, notification, cancellationToken).AsTask(), cancellationToken);
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a single handler with exception isolation.
        /// </summary>
        private async ValueTask ExecuteHandlerSafely<TNotification>(
            NotificationHandlerRegistration handlerRegistration,
            TNotification notification,
            CancellationToken cancellationToken)
            where TNotification : INotification
        {
            try
            {
                using var scope = CreateScope();
                var handler = handlerRegistration.HandlerFactory(scope.ServiceProvider);
                
                _logger?.LogTrace("Executing handler {HandlerType} for notification {NotificationType}",
                    handlerRegistration.HandlerType.Name, typeof(TNotification).Name);

                await handlerRegistration.ExecuteHandler(handler, notification, cancellationToken);
                
                _logger?.LogTrace("Successfully executed handler {HandlerType} for notification {NotificationType}",
                    handlerRegistration.HandlerType.Name, typeof(TNotification).Name);
            }
            catch (Exception ex) when (_options.ContinueOnException)
            {
                _logger?.LogError(ex, "Handler {HandlerType} failed while processing notification {NotificationType}. Continuing with remaining handlers.",
                    handlerRegistration.HandlerType.Name, typeof(TNotification).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Handler {HandlerType} failed while processing notification {NotificationType}. Stopping execution.",
                    handlerRegistration.HandlerType.Name, typeof(TNotification).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets the registered handlers for a notification type.
        /// </summary>
        /// <param name="notificationType">The notification type.</param>
        /// <returns>The list of registered handlers, or empty list if none found.</returns>
        public IReadOnlyList<NotificationHandlerRegistration> GetHandlers(Type notificationType)
        {
            return _handlerRegistrations.TryGetValue(notificationType, out var handlers) 
                ? handlers.AsReadOnly() 
                : Array.Empty<NotificationHandlerRegistration>();
        }

        /// <summary>
        /// Gets all registered notification types.
        /// </summary>
        /// <returns>The collection of registered notification types.</returns>
        public IReadOnlyCollection<Type> GetRegisteredNotificationTypes()
        {
            return _handlerRegistrations.Keys.ToList().AsReadOnly();
        }
    }
}