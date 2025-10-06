using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Publishing
{
    /// <summary>
    /// Defines the strategy for publishing notifications to handlers.
    /// Different implementations can use different dispatching strategies
    /// (e.g., sequential, parallel, custom ordering).
    /// </summary>
    public interface INotificationPublisher
    {
        /// <summary>
        /// Publishes a notification to all registered handlers using the publisher's strategy.
        /// </summary>
        /// <typeparam name="TNotification">The type of notification.</typeparam>
        /// <param name="notification">The notification to publish.</param>
        /// <param name="handlers">The collection of handlers to invoke.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the publish operation.</returns>
        ValueTask PublishAsync<TNotification>(
            TNotification notification,
            IEnumerable<INotificationHandler<TNotification>> handlers,
            CancellationToken cancellationToken)
            where TNotification : INotification;
    }
}
