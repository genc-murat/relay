using System;

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
}