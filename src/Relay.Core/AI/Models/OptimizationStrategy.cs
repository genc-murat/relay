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
        
        /// <summary>Batch multiple requests together</summary>
        BatchProcessing,
        
        /// <summary>Use async enumerable for streaming</summary>
        StreamingOptimization,
        
        /// <summary>Apply memory pooling</summary>
        MemoryPooling,
        
        /// <summary>Use parallel processing</summary>
        ParallelProcessing,
        
        /// <summary>Apply circuit breaker pattern</summary>
        CircuitBreaker,
        
        /// <summary>Optimize database queries</summary>
        DatabaseOptimization,
        
        /// <summary>Use SIMD acceleration</summary>
        SIMDAcceleration,
        
        /// <summary>Apply custom optimization</summary>
        Custom
    }
}