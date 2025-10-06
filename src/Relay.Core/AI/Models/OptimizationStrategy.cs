namespace Relay.Core.AI
{
    /// <summary>
    /// Available optimization strategies.
    /// </summary>
    public enum OptimizationStrategy
    {
        /// <summary>No optimization needed</summary>
        None,

        /// <summary>Enable caching for this request type</summary>
        EnableCaching,

        /// <summary>Enable caching (alias)</summary>
        Caching = EnableCaching,

        /// <summary>Batch multiple requests together</summary>
        BatchProcessing,

        /// <summary>Batching (alias)</summary>
        Batching = BatchProcessing,

        /// <summary>Use async enumerable for streaming</summary>
        StreamingOptimization,

        /// <summary>Apply memory pooling</summary>
        MemoryPooling,

        /// <summary>Use parallel processing</summary>
        ParallelProcessing,

        /// <summary>Parallelization (alias)</summary>
        Parallelization = ParallelProcessing,

        /// <summary>Apply circuit breaker pattern</summary>
        CircuitBreaker,

        /// <summary>Optimize database queries</summary>
        DatabaseOptimization,

        /// <summary>Use SIMD acceleration</summary>
        SIMDAcceleration,

        /// <summary>Apply custom optimization</summary>
        Custom,

        /// <summary>Apply compression optimization</summary>
        CompressionOptimization,

        /// <summary>Apply memory optimization</summary>
        MemoryOptimization,

        /// <summary>Apply lazy loading</summary>
        LazyLoading,

        /// <summary>Apply resource pooling</summary>
        ResourcePooling
    }
}