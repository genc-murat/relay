namespace Relay.Core.AI.Pipeline.Interfaces
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