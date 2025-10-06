using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    /// <summary>
    /// Base implementation of notification dispatcher with common functionality.
    /// Generated dispatchers will inherit from this class.
    /// </summary>
    public abstract class BaseNotificationDispatcher : BaseDispatcher, INotificationDispatcher
    {
        /// <summary>
        /// Initializes a new instance of the BaseNotificationDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        protected BaseNotificationDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <inheritdoc />
        public abstract ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
            where TNotification : INotification;

        /// <summary>
        /// Executes multiple notification handlers in parallel.
        /// </summary>
        /// <param name="handlers">The handlers to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A ValueTask representing the completion of all handlers.</returns>
        protected static async ValueTask ExecuteHandlersParallel(IEnumerable<ValueTask> handlers, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                tasks.Add(handler.AsTask());
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Executes multiple notification handlers sequentially.
        /// </summary>
        /// <param name="handlers">The handlers to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A ValueTask representing the completion of all handlers.</returns>
        protected static async ValueTask ExecuteHandlersSequential(IEnumerable<ValueTask> handlers, CancellationToken cancellationToken)
        {
            foreach (var handler in handlers)
            {
                await handler;
            }
        }
    }
}