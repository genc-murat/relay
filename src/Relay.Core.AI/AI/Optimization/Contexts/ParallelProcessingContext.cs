namespace Relay.Core.AI.Optimization.Contexts
{
    /// <summary>
    /// Context for parallel processing operations.
    /// </summary>
    public sealed class ParallelProcessingContext
    {
        public int MaxDegreeOfParallelism { get; init; }
        public bool EnableWorkStealing { get; init; }
        public int MinItemsForParallel { get; init; }
        public double CpuUtilization { get; init; }
        public int AvailableProcessors { get; init; }
    }
}
