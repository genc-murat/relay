namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for batch coordinators.
    /// </summary>
    internal interface IBatchCoordinator
    {
        BatchCoordinatorMetadata? GetMetadata();
    }
}
