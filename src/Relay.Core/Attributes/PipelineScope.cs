namespace Relay.Core
{
    /// <summary>
    /// Defines the scope for pipeline behaviors.
    /// </summary>
    public enum PipelineScope
    {
        /// <summary>
        /// Apply to all request types.
        /// </summary>
        All,

        /// <summary>
        /// Apply only to regular requests.
        /// </summary>
        Requests,

        /// <summary>
        /// Apply only to streaming requests.
        /// </summary>
        Streams,

        /// <summary>
        /// Apply only to notifications.
        /// </summary>
        Notifications
    }
}