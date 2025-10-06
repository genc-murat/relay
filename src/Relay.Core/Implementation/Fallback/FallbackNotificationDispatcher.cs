using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Performance;

namespace Relay.Core
{

    /// <summary>
    /// Fallback notification dispatcher that uses reflection when no generated dispatcher is available.
    /// This provides basic functionality but with lower performance than generated dispatchers.
    /// </summary>
    public class FallbackNotificationDispatcher : BaseNotificationDispatcher
    {
        /// <summary>
        /// Initializes a new instance of the FallbackNotificationDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        public FallbackNotificationDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        {
            ValidateRequest(notification);

            try
            {
                // Performance optimization: Use pre-allocated arrays and avoid LINQ allocations
                var handlers = ServiceProvider.GetServices<INotificationHandler<TNotification>>();

                // Fast path: no handlers
                if (!handlers.Any())
                    return;

                // Convert to array once to avoid multiple enumerations
                var handlerArray = handlers as INotificationHandler<TNotification>[] ?? handlers.ToArray();

                // Single handler fast path
                if (handlerArray.Length == 1)
                {
                    await handlerArray[0].HandleAsync(notification, cancellationToken);
                    return;
                }

                // Multiple handlers - create ValueTask array directly
                var tasks = new ValueTask[handlerArray.Length];
                for (int i = 0; i < handlerArray.Length; i++)
                {
                    tasks[i] = handlerArray[i].HandleAsync(notification, cancellationToken);
                }

                // Execute handlers in parallel using ValueTask array directly
                await ExecuteHandlersParallel(tasks, cancellationToken);
            }
            catch (Exception ex)
            {
                throw HandleException(ex, typeof(TNotification).Name);
            }
        }
    }
}