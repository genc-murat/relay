namespace Relay.Core.AI.Optimization.Batching
{
    /// <summary>
    /// Interface for batch coordinators.
    /// </summary>
    internal interface IBatchCoordinator
    {
        BatchCoordinatorMetadata? Metadata { get; set; }
        BatchCoordinatorMetadata? GetMetadata();
    }
}
