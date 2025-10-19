namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for tracking active requests.
    /// </summary>
    public interface IActiveRequestCounter
    {
        int GetActiveRequestCount();
        int GetQueuedRequestCount();
    }
}