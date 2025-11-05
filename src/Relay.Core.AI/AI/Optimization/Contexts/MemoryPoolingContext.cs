namespace Relay.Core.AI.Optimization.Contexts
{
    /// <summary>
    /// Context for memory pooling operations.
    /// </summary>
    public sealed class MemoryPoolingContext
    {
        public bool EnableObjectPooling { get; init; }
        public bool EnableBufferPooling { get; init; }
        public int EstimatedBufferSize { get; init; } = 4096;
    }
}
