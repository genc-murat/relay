namespace Relay.Core.AI
{
    /// <summary>
    /// Available parallelization strategies.
    /// </summary>
    public enum ParallelizationStrategy
    {
        None,
        Static,
        Dynamic,
        WorkStealing,
        AIPredictive
    }
}