namespace Relay.Core
{
    /// <summary>
    /// Defines the dispatch mode for notification handlers.
    /// </summary>
    public enum NotificationDispatchMode
    {
        /// <summary>
        /// Execute notification handlers in parallel.
        /// </summary>
        Parallel,

        /// <summary>
        /// Execute notification handlers sequentially.
        /// </summary>
        Sequential
    }
}