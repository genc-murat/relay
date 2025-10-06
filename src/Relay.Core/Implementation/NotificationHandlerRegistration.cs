using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
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
}