using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Publishing
{
    /// <summary>
    /// Notification publisher that respects handler ordering attributes and dependencies.
    /// Provides MediatR-compatible handler ordering with additional features like groups and dependencies.
    /// </summary>
    /// <remarks>
    /// This publisher analyzes handler attributes to determine execution order:
    /// 1. NotificationHandlerOrderAttribute - explicit ordering (lower values first)
    /// 2. NotificationHandlerGroupAttribute - group-based execution
    /// 3. ExecuteAfterAttribute / ExecuteBeforeAttribute - dependency-based ordering
    /// 4. NotificationExecutionModeAttribute - execution mode control
    /// 
    /// Handlers are sorted and executed according to these rules, providing fine-grained control
    /// over notification processing flow.
    /// </remarks>
    public class OrderedNotificationPublisher : INotificationPublisher
    {
        private readonly ILogger<OrderedNotificationPublisher>? _logger;
        private readonly bool _continueOnException;
        private readonly int _maxDegreeOfParallelism;

        /// <summary>
        /// Initializes a new instance of the OrderedNotificationPublisher class.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        /// <param name="continueOnException">Whether to continue executing handlers if one throws an exception.</param>
        /// <param name="maxDegreeOfParallelism">Maximum number of handlers to execute in parallel.</param>
        public OrderedNotificationPublisher(
            ILogger<OrderedNotificationPublisher>? logger = null,
            bool continueOnException = true,
            int maxDegreeOfParallelism = -1)
        {
            _logger = logger;
            _continueOnException = continueOnException;
            _maxDegreeOfParallelism = maxDegreeOfParallelism <= 0
                ? Environment.ProcessorCount
                : maxDegreeOfParallelism;
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

            var handlersList = handlers.ToList();
            if (handlersList.Count == 0)
            {
                _logger?.LogDebug("No handlers registered for notification {NotificationType}",
                    typeof(TNotification).Name);
                return;
            }

            _logger?.LogDebug(
                "Publishing notification {NotificationType} to {HandlerCount} handler(s) with ordering",
                typeof(TNotification).Name,
                handlersList.Count);

            // Analyze and order handlers
            var orderedHandlers = AnalyzeAndOrderHandlers(handlersList);

            // Execute handlers according to their execution groups
            await ExecuteOrderedHandlers(notification, orderedHandlers, cancellationToken);
        }

        /// <summary>
        /// Analyzes handler attributes and creates an ordered execution plan.
        /// </summary>
        private List<HandlerExecutionInfo<TNotification>> AnalyzeAndOrderHandlers<TNotification>(
            List<INotificationHandler<TNotification>> handlers)
            where TNotification : INotification
        {
            var handlerInfos = new List<HandlerExecutionInfo<TNotification>>();

            foreach (var handler in handlers)
            {
                var handlerType = handler.GetType();
                var info = new HandlerExecutionInfo<TNotification>
                {
                    Handler = handler,
                    HandlerType = handlerType,
                    Order = GetHandlerOrder(handlerType),
                    Group = GetHandlerGroup(handlerType),
                    GroupOrder = GetGroupOrder(handlerType),
                    ExecutionMode = GetExecutionMode(handlerType),
                    Dependencies = GetDependencies(handlerType),
                    AllowParallelExecution = GetAllowParallelExecution(handlerType),
                    SuppressExceptions = GetSuppressExceptions(handlerType)
                };

                handlerInfos.Add(info);
            }

            // Resolve dependencies and sort
            var sorted = TopologicalSort(handlerInfos);

            _logger?.LogTrace("Handler execution order:");
            for (int i = 0; i < sorted.Count; i++)
            {
                _logger?.LogTrace("  {Index}. {HandlerType} (Order: {Order}, Group: {Group})",
                    i + 1,
                    sorted[i].HandlerType.Name,
                    sorted[i].Order,
                    sorted[i].Group ?? "None");
            }

            return sorted;
        }

        /// <summary>
        /// Executes handlers in their determined order, respecting groups and execution modes.
        /// </summary>
        private async ValueTask ExecuteOrderedHandlers<TNotification>(
            TNotification notification,
            List<HandlerExecutionInfo<TNotification>> orderedHandlers,
            CancellationToken cancellationToken)
            where TNotification : INotification
        {
            // Group handlers by their group order
            var groupedHandlers = orderedHandlers
                .GroupBy(h => h.GroupOrder)
                .OrderBy(g => g.Key);

            foreach (var group in groupedHandlers)
            {
                _logger?.LogTrace("Executing handler group {GroupOrder}", group.Key);

                // Within each group, separate by execution mode
                var sequentialHandlers = group.Where(h =>
                    !h.AllowParallelExecution ||
                    h.ExecutionMode == NotificationExecutionMode.Sequential).ToList();

                var parallelHandlers = group.Where(h =>
                    h.AllowParallelExecution &&
                    h.ExecutionMode != NotificationExecutionMode.Sequential).ToList();

                // Execute sequential handlers first
                foreach (var handlerInfo in sequentialHandlers.OrderBy(h => h.Order))
                {
                    await ExecuteHandler(notification, handlerInfo, cancellationToken);
                }

                // Execute parallel handlers concurrently
                if (parallelHandlers.Any())
                {
                    await ExecuteHandlersInParallel(notification, parallelHandlers, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Executes multiple handlers in parallel.
        /// </summary>
        private async ValueTask ExecuteHandlersInParallel<TNotification>(
            TNotification notification,
            List<HandlerExecutionInfo<TNotification>> handlers,
            CancellationToken cancellationToken)
            where TNotification : INotification
        {
            if (handlers.Count == 1)
            {
                await ExecuteHandler(notification, handlers[0], cancellationToken);
                return;
            }

            var tasks = handlers.Select(h =>
                ExecuteHandler(notification, h, cancellationToken).AsTask()).ToArray();

            if (_continueOnException)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                // Stop on first exception
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes a single handler with exception handling.
        /// </summary>
        private async ValueTask ExecuteHandler<TNotification>(
            TNotification notification,
            HandlerExecutionInfo<TNotification> handlerInfo,
            CancellationToken cancellationToken)
            where TNotification : INotification
        {
            try
            {
                _logger?.LogTrace("Executing handler {HandlerType} for notification {NotificationType}",
                    handlerInfo.HandlerType.Name,
                    typeof(TNotification).Name);

                await handlerInfo.Handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);

                _logger?.LogTrace("Handler {HandlerType} completed successfully",
                    handlerInfo.HandlerType.Name);
            }
            catch (Exception ex)
            {
                if (handlerInfo.SuppressExceptions || _continueOnException)
                {
                    _logger?.LogError(ex,
                        "Handler {HandlerType} failed for notification {NotificationType}. Continuing.",
                        handlerInfo.HandlerType.Name,
                        typeof(TNotification).Name);
                }
                else
                {
                    _logger?.LogError(ex,
                        "Handler {HandlerType} failed for notification {NotificationType}. Stopping execution.",
                        handlerInfo.HandlerType.Name,
                        typeof(TNotification).Name);
                    throw;
                }
            }
        }

        #region Attribute Reading

        private static int GetHandlerOrder(Type handlerType)
        {
            var attr = handlerType.GetCustomAttribute<NotificationHandlerOrderAttribute>();
            return attr?.Order ?? 0;
        }

        private static string? GetHandlerGroup(Type handlerType)
        {
            var attr = handlerType.GetCustomAttribute<NotificationHandlerGroupAttribute>();
            return attr?.GroupName;
        }

        private static int GetGroupOrder(Type handlerType)
        {
            var attr = handlerType.GetCustomAttribute<NotificationHandlerGroupAttribute>();
            return attr?.GroupOrder ?? 0;
        }

        private static NotificationExecutionMode GetExecutionMode(Type handlerType)
        {
            var attr = handlerType.GetCustomAttribute<NotificationExecutionModeAttribute>();
            return attr?.Mode ?? NotificationExecutionMode.Default;
        }

        private static HashSet<Type> GetDependencies(Type handlerType)
        {
            var dependencies = new HashSet<Type>();

            // ExecuteAfter dependencies
            var executeAfterAttrs = handlerType.GetCustomAttributes<ExecuteAfterAttribute>();
            foreach (var attr in executeAfterAttrs)
            {
                dependencies.Add(attr.HandlerType);
            }

            // ExecuteBefore creates reverse dependencies
            var executeBeforeAttrs = handlerType.GetCustomAttributes<ExecuteBeforeAttribute>();
            // These are handled in TopologicalSort

            return dependencies;
        }

        private static bool GetAllowParallelExecution(Type handlerType)
        {
            var attr = handlerType.GetCustomAttribute<NotificationExecutionModeAttribute>();
            return attr?.AllowParallelExecution ?? true;
        }

        private static bool GetSuppressExceptions(Type handlerType)
        {
            var attr = handlerType.GetCustomAttribute<NotificationExecutionModeAttribute>();
            return attr?.SuppressExceptions ?? false;
        }

        #endregion

        #region Topological Sort

        /// <summary>
        /// Performs topological sort on handlers based on their dependencies.
        /// </summary>
        private List<HandlerExecutionInfo<TNotification>> TopologicalSort<TNotification>(
            List<HandlerExecutionInfo<TNotification>> handlers)
            where TNotification : INotification
        {
            // Build dependency graph
            var graph = new Dictionary<Type, HashSet<Type>>();
            var reverseDependencies = new Dictionary<Type, HashSet<Type>>();

            foreach (var handler in handlers)
            {
                graph[handler.HandlerType] = new HashSet<Type>(handler.Dependencies);
                if (!reverseDependencies.ContainsKey(handler.HandlerType))
                {
                    reverseDependencies[handler.HandlerType] = new HashSet<Type>();
                }
            }

            // Add reverse dependencies from ExecuteBefore attributes
            foreach (var handler in handlers)
            {
                var executeBeforeAttrs = handler.HandlerType.GetCustomAttributes<ExecuteBeforeAttribute>();
                foreach (var attr in executeBeforeAttrs)
                {
                    if (graph.ContainsKey(attr.HandlerType))
                    {
                        graph[attr.HandlerType].Add(handler.HandlerType);
                    }
                }
            }

            // Perform topological sort using Kahn's algorithm
            var sorted = new List<HandlerExecutionInfo<TNotification>>();
            var visited = new HashSet<Type>();
            var queue = new Queue<HandlerExecutionInfo<TNotification>>();

            // Start with handlers that have no dependencies
            foreach (var handler in handlers.OrderBy(h => h.GroupOrder).ThenBy(h => h.Order))
            {
                if (!graph[handler.HandlerType].Any())
                {
                    queue.Enqueue(handler);
                    visited.Add(handler.HandlerType);
                }
            }

            while (queue.Count > 0)
            {
                var handler = queue.Dequeue();
                sorted.Add(handler);

                // Find handlers that depend on this one
                foreach (var other in handlers)
                {
                    if (visited.Contains(other.HandlerType))
                        continue;

                    if (graph[other.HandlerType].Contains(handler.HandlerType))
                    {
                        graph[other.HandlerType].Remove(handler.HandlerType);

                        if (!graph[other.HandlerType].Any())
                        {
                            queue.Enqueue(other);
                            visited.Add(other.HandlerType);
                        }
                    }
                }
            }

            // Check for circular dependencies
            if (sorted.Count != handlers.Count)
            {
                var remaining = handlers.Where(h => !visited.Contains(h.HandlerType)).ToList();
                _logger?.LogWarning(
                    "Circular dependency detected in notification handlers: {Handlers}",
                    string.Join(", ", remaining.Select(h => h.HandlerType.Name)));

                // Add remaining handlers at the end
                sorted.AddRange(remaining);
            }

            return sorted;
        }

        #endregion

        /// <summary>
        /// Contains execution information for a notification handler.
        /// </summary>
        private class HandlerExecutionInfo<TNotification>
            where TNotification : INotification
        {
            public INotificationHandler<TNotification> Handler { get; set; } = null!;
            public Type HandlerType { get; set; } = null!;
            public int Order { get; set; }
            public string? Group { get; set; }
            public int GroupOrder { get; set; }
            public NotificationExecutionMode ExecutionMode { get; set; }
            public HashSet<Type> Dependencies { get; set; } = new HashSet<Type>();
            public bool AllowParallelExecution { get; set; }
            public bool SuppressExceptions { get; set; }
        }
    }
}
